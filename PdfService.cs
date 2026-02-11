using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using System.Collections.Generic;
using static Harjoitustyö.Uusi_Lasku;
using static Harjoitustyö.Tietokanta;
using static Harjoitustyö.Barcode;

namespace Harjoitustyö
{
    public static class PdfService
    {
        public static void LuoPDF(Lasku lasku, string tiedostoNimi)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // 1. Esikäsitellään data: Yhdistetään samanlaiset rivit
            var tiivistetytRivit = lasku.Tuotteet
                .GroupBy(t => new { t.Nimi, t.Yksikkö, t.A_Hinta, t.ALV })
                .Select(g => new
                {
                    Nimi = g.Key.Nimi,
                    Yksikkö = g.Key.Yksikkö,
                    A_Hinta = g.Key.A_Hinta,
                    ALV_Prosentti = g.Key.ALV,
                    Määrä = g.Sum(t => t.Määrä),
                    YhteensäVeroton = g.Sum(t => t.Määrä) * g.Key.A_Hinta,
                    ALV_Euro = g.Sum(t => (decimal)t.ALV / 100 * t.A_Hinta * t.Määrä),
                    Yhteensä = g.Sum(t => (t.Määrä * t.A_Hinta) + ((decimal)t.ALV / 100 * t.A_Hinta * t.Määrä))
                })
                .ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // --- YLÄTUNNISTE (HEADER) ---
                    page.Header().PaddingBottom(20).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("LASKU").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text($"Laskun numero: {lasku.LaskunNumero}").FontSize(12);
                            col.Item().Text($"Päiväys: {lasku.Päiväys:dd.MM.yyyy}");
                            col.Item().Text($"Eräpäivä: {lasku.Eräpäivä:dd.MM.yyyy}").SemiBold();
                        });

                        var barcodeData = GetBarcodeBytes(lasku.LaskunNumero.ToString());
                        row.ConstantItem(120).AlignRight().Image(barcodeData).FitArea();
                    });

                    // --- SISÄLTÖ (CONTENT) ---
                    page.Content().Column(col =>
                    {
                        // OSOITEKENTÄT
                        col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(20).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Laskuttaja").Label();
                                c.Item().Text(lasku.LaskuttajaInfo.Nimi).Bold();
                                c.Item().Text(lasku.LaskuttajaInfo.Osoite);
                                c.Item().Text(lasku.LaskuttajaInfo.Postinumero);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Vastaanottaja").Label();
                                c.Item().Text(lasku.AsiakasInfo.Nimi).Bold();
                                c.Item().Text(lasku.AsiakasInfo.Osoite);
                                c.Item().Text(lasku.AsiakasInfo.Postinumero);
                            });
                        });

                        col.Item().PaddingTop(20);

                        // TUOTETAULUKKO
                        col.Item().Table(table =>
                        {
                            // Määritellään sarakkeet. 
                            // ALV-sarakkeen suhdeluku nostettu 1 -> 2, jotta eurot mahtuvat.
                            // Tuote-sarakkeesta otettu yksi pois (4 -> 3).
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Tuote
                                columns.RelativeColumn(1); // Määrä
                                columns.RelativeColumn(1); // Yksikkö
                                columns.RelativeColumn(2); // a-hinta
                                columns.RelativeColumn(2); // ALV € (Levennetty)
                                columns.RelativeColumn(2); // Yhteensä
                            });

                            // Otsikkorivi
                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Tuote");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Määrä");
                                header.Cell().Element(HeaderStyle).Text("yks.");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("hinta");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("ALV €");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Yhteensä");
                            });

                            // Datarivit
                            foreach (var rivi in tiivistetytRivit)
                            {
                                table.Cell().Element(CellStyle).Text(rivi.Nimi);
                                table.Cell().Element(CellStyle).AlignRight().Text(rivi.Määrä.ToString());
                                table.Cell().Element(CellStyle).Text(rivi.Yksikkö);
                                table.Cell().Element(CellStyle).AlignRight().Text($"{rivi.A_Hinta:N2} €");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{rivi.ALV_Euro:N2} €");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{rivi.Yhteensä:N2} €").SemiBold();
                            }

                            // YHTEENVETO
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(6).PaddingTop(10).AlignRight().Column(c =>
                                {
                                    c.Item().Row(r =>
                                    {
                                        r.RelativeItem().AlignRight().Text("Veroton yhteensä:").FontSize(10);
                                        r.ConstantItem(100).AlignRight().Text($"{tiivistetytRivit.Sum(x => x.YhteensäVeroton):N2} €");
                                    });
                                    c.Item().Row(r =>
                                    {
                                        r.RelativeItem().AlignRight().Text("ALV yhteensä:").FontSize(10);
                                        r.ConstantItem(100).AlignRight().Text($"{tiivistetytRivit.Sum(x => x.ALV_Euro):N2} €");
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.RelativeItem().AlignRight().Text("Yhteensä (sis. ALV):").FontSize(12).Bold();
                                        r.ConstantItem(100).AlignRight().Text($"{lasku.Yhteensä:N2} €").FontSize(12).Bold();
                                    });
                                });
                            });
                        });

                        // ALV-ERITTELY
                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(80);
                                cols.ConstantColumn(80);
                                cols.ConstantColumn(80);
                                cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("ALV %").Bold().FontSize(9);
                                header.Cell().AlignRight().Text("Veroton").Bold().FontSize(9);
                                header.Cell().AlignRight().Text("Vero").Bold().FontSize(9);
                                header.Cell().AlignRight().Text("Yhteensä").Bold().FontSize(9);
                            });

                            var alvKannat = tiivistetytRivit.GroupBy(x => x.ALV_Prosentti);
                            foreach (var kanta in alvKannat)
                            {
                                table.Cell().Text($"{kanta.Key} %").FontSize(9);
                                table.Cell().AlignRight().Text($"{kanta.Sum(x => x.YhteensäVeroton):N2}").FontSize(9);
                                table.Cell().AlignRight().Text($"{kanta.Sum(x => x.ALV_Euro):N2}").FontSize(9);
                                table.Cell().AlignRight().Text($"{kanta.Sum(x => x.Yhteensä):N2}").FontSize(9);
                            }
                        });

                        // LISÄTIEDOT
                        if (!string.IsNullOrWhiteSpace(lasku.AsiakasInfo.Lisätiedot))
                        {
                            col.Item().PaddingTop(20).Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                            {
                                c.Item().Text("Lisätiedot:").Bold().FontSize(10);
                                c.Item().Text(lasku.AsiakasInfo.Lisätiedot).FontSize(10);
                            });
                        }
                    });

                    // --- ALATUNNISTE ---
                    page.Footer().PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Rakennus OY | ").Bold();
                            t.Span("Rakennustie 15, 00100 Helsinki | ");
                            t.Span("www.rakennusoy.fi");
                        });

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Sivu ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            })
            .GeneratePdf(tiedostoNimi);
        }

        // --- TYYLIT ---
        static IContainer HeaderStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken1)
                .Background(Colors.Grey.Lighten3)
                .Padding(5)
                .DefaultTextStyle(x => x.SemiBold());
        }

        static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(5);
        }

        static void Label(this TextBlockDescriptor text) => text.FontSize(10).FontColor(Colors.Grey.Darken1);
    }
}