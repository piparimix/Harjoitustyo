using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using Harjoitustyö; // Importtaa namespace, jossa Lasku nyt asuu

namespace Harjoitustyö
{
    public static class PdfService
    {
        // Viittaa nyt suoraan Lasku-luokkaan, ei Uusi_Lasku.Lasku
        public static void LuoPDF(Lasku lasku, string tiedostoNimi)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text($"Lasku {lasku.LaskunNumero}").SemiBold().FontSize(20);
                            column.Item().Text($"Päiväys: {lasku.Päiväys:dd.MM.yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Text($"Asiakas: {lasku.AsiakasInfo.Nimi}");
                        col.Item().Text(lasku.AsiakasInfo.Osoite);

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Tuote").Bold();
                                header.Cell().AlignRight().Text("Määrä").Bold();
                                header.Cell().AlignRight().Text("Hinta").Bold();
                                header.Cell().AlignRight().Text("Yhteensä").Bold();
                            });

                            foreach (var tuote in lasku.Tuotteet)
                            {
                                table.Cell().Text(tuote.Nimi);
                                table.Cell().AlignRight().Text($"{tuote.Määrä} {tuote.Yksikkö}");
                                table.Cell().AlignRight().Text($"{tuote.A_Hinta:C}");
                                table.Cell().AlignRight().Text($"{tuote.Yhteensä:C}");
                            }
                        });

                        col.Item().PaddingTop(10).AlignRight().Text($"Yhteensä: {lasku.Yhteensä:C}").FontSize(14).Bold();
                    });

                    page.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
                });
            })
            .GeneratePdf(tiedostoNimi);
        }
    }
}