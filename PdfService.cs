using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq; 
using static Harjoitustyö.Uusi_Lasku;
using static Harjoitustyö.Tietokanta;
using static Harjoitustyö.Barcode;

namespace Harjoitustyö
{
    public static class PdfService
    {
        // Metodi PDF-laskun luomiseen, käyttäen QuestPDF-kirjastoa ja Lasku-olion tietoja
        public static void LuoPDF(Lasku lasku, string tiedostoNimi)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // 1. OTSIKKO JA VIIVAKOODI
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text($"Lasku {lasku.LaskunNumero}").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                            column.Item().Text($"Päiväys: {lasku.Päiväys:dd.MM.yyyy}");
                            column.Item().Text($"Eräpäivä: {lasku.Eräpäivä:dd.MM.yyyy}");
                        });

                        var barcodeData = GetBarcodeBytes(lasku.LaskunNumero.ToString());
                        row.ConstantItem(150).Image(barcodeData);
                    });

                    // 2. SISÄLTÖ (Osoitteet, Taulukko ja Summa saman Content-lohkon sisällä)
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // OSOITETIEDOT
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("Laskuttaja").Bold();
                                column.Item().Text(lasku.LaskuttajaInfo.Nimi);
                                column.Item().Text(lasku.LaskuttajaInfo.Osoite);
                                column.Item().Text(lasku.LaskuttajaInfo.Postinumero);
                            });

                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("Asiakas").Bold();
                                column.Item().Text(lasku.AsiakasInfo.Nimi);
                                column.Item().Text(lasku.AsiakasInfo.Osoite);
                                column.Item().Text(lasku.AsiakasInfo.Postinumero);
                            });
                        });

                        col.Item().PaddingTop(1, Unit.Centimetre);

                        // TUOTETAULUKKO 
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Tuote").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Määrä").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Yksikkö").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Hinta").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Yhteensä").Bold();
                            });

                            foreach (var tuote in lasku.Tuotteet)
                            {
                                table.Cell().PaddingVertical(2).Text(tuote.Nimi);
                                table.Cell().PaddingVertical(2).Text(tuote.Määrä.ToString());
                                table.Cell().PaddingVertical(2).Text(tuote.Yksikkö);
                                table.Cell().PaddingVertical(2).Text($"{tuote.A_Hinta:C}");
                                table.Cell().PaddingVertical(2).Text($"{tuote.Yhteensä:C}");
                            }
                        });

                        // LOPPUSUMMAT
                        col.Item().PaddingTop(10).AlignRight().Column(sumCol =>
                        {
                            sumCol.Item().Text(text =>
                            {
                                text.Span("Yhteensä (sis. ALV): ").FontSize(14);
                                text.Span($"{lasku.Yhteensä:C}").FontSize(16).Bold();
                            });

                            decimal alvOsuus = lasku.Tuotteet.Sum(t => t.ALV_Euro);
                            sumCol.Item().Text($"Josta ALV: {alvOsuus:C}").FontSize(10).Italic();
                        });

                        // Mahdolliset lisätiedot
                        if (!string.IsNullOrWhiteSpace(lasku.AsiakasInfo.Lisätiedot))
                        {
                            col.Item().PaddingTop(20).Text(text => {
                                text.Span("Lisätiedot: ").Bold();
                                text.Span(lasku.AsiakasInfo.Lisätiedot);
                            });
                        }
                    });

                    // 3. ALATUNNISTE
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sivu ");
                        x.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf(tiedostoNimi);
        }
    }
}