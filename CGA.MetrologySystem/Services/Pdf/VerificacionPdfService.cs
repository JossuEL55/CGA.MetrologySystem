using CGA.MetrologySystem.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace CGA.MetrologySystem.Services.Pdf
{
    public class VerificacionPdfService
    {
        public byte[] Generar(EventoVerificacionDato verificacion)
        {
            var evento = verificacion.EventoMetrologico;
            var equipo = evento.Equipo;
            var responsable = evento.ResponsableInterno;

            var resultados = evento.ResultadosVerificacion
                .OrderBy(r => r.Orden)
                .ToList();

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Content().Column(column =>
                    {
                        column.Item().Element(Encabezado);
                        column.Item().Element(container => DatosEquipo(container, equipo));
                        column.Item().Element(container => FechaYFrecuencia(container, evento));
                        column.Item().Element(container => TablaResultados(container, resultados));
                        column.Item().Element(container => EstadoResponsableComentarios(container, evento, responsable));
                        column.Item().Element(container => Firmas(container));
                    });
                });
            }).GeneratePdf();
        }

        private void Encabezado(IContainer container)
        {
            var logoPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "logo.png"
            );

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(6);
                    columns.RelativeColumn(2);
                });

                table.Cell()
                    .Border(0.5f)
                    .Height(75)
                    .Padding(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Image(logoPath)
                    .FitArea();

                table.Cell()
                    .Border(0.5f)
                    .Height(75)
                    .AlignCenter()
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().AlignCenter().Text("CGA OIL INSPECTION SERVICES")
                            .FontSize(9)
                            .SemiBold();

                        col.Item().PaddingTop(4).AlignCenter().Text("VERIFICACIÓN DE EQUIPOS")
                            .FontSize(17)
                            .Bold();

                        col.Item().PaddingTop(4).AlignCenter().Text("NTE INEN ISO/IEC 17020")
                            .FontSize(8)
                            .Italic();
                    });

                table.Cell()
                    .Border(0.5f)
                    .Height(75)
                    .Table(inner =>
                    {
                        inner.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        FilaMeta(inner, "Código:", "CGA-FOR-025");
                        FilaMeta(inner, "Fecha:", "2025-04-21");
                        FilaMeta(inner, "Revisión:", "1");
                        FilaMeta(inner, "Página:", "1 de 1");
                    });
            });
        }

        private void DatosEquipo(IContainer container, Equipo equipo)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell()
                    .ColumnSpan(2)
                    .Border(0.5f)
                    .Background("#E0E0E0")
                    .PaddingVertical(4)
                    .AlignCenter()
                    .Text("DATOS DEL EQUIPO")
                    .Bold()
                    .FontSize(9);

                table.Cell().Border(0.5f).Padding(7).Column(col =>
                {
                    Campo(col, "Equipo:", equipo.Nombre);
                    Campo(col, "Marca:", equipo.Marca);
                    Campo(col, "Modelo:", equipo.Modelo);
                    Campo(col, "Serie:", equipo.Serie);
                });

                table.Cell().Border(0.5f).Padding(7).Column(col =>
                {
                    Campo(col, "Identificación:", equipo.Codigo);
                    Campo(col, "Rango:", ObtenerCaracteristica(equipo, "Rango"));
                    Campo(col, "Exactitud/e.m.p:", ObtenerCaracteristica(equipo, "Exactitud"));
                });
            });
        }

        private void FechaYFrecuencia(IContainer container, EventoMetrologico evento)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell()
                    .Border(0.5f)
                    .PaddingVertical(3)
                    .AlignCenter()
                    .Text($"FECHA: {evento.FechaEvento:yyyy-MM-dd}")
                    .Bold()
                    .FontSize(9);

                table.Cell()
                    .Border(0.5f)
                    .PaddingVertical(3)
                    .AlignCenter()
                    .Text("FRECUENCIA DE VERIFICACIÓN: 4 MESES")
                    .Bold()
                    .FontSize(9);
            });
        }

        private void TablaResultados(IContainer container, List<EventoVerificacionResultado> resultados)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(5);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                });

                HeaderCell(table, "Verificación, Inspección visual (Condiciones a verificar)");
                HeaderCell(table, "Cumple");
                HeaderCell(table, "No cumple");
                HeaderCell(table, "Observaciones");

                foreach (var resultado in resultados)
                {
                    table.Cell()
                        .Border(0.5f)
                        .Padding(6)
                        .MinHeight(34)
                        .AlignMiddle()
                        .Text(resultado.DescripcionItem)
                        .FontSize(8);

                    table.Cell()
                        .Border(0.5f)
                        .Padding(6)
                        .MinHeight(34)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(resultado.Cumple ? "X" : "")
                        .Bold()
                        .FontSize(9);

                    table.Cell()
                        .Border(0.5f)
                        .Padding(6)
                        .MinHeight(34)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(!resultado.Cumple ? "X" : "")
                        .Bold()
                        .FontSize(9);

                    table.Cell()
                        .Border(0.5f)
                        .Padding(6)
                        .MinHeight(34)
                        .AlignMiddle()
                        .Text(resultado.Observaciones ?? "")
                        .FontSize(8);
                }

                var filasVacias = Math.Max(0, 7 - resultados.Count);

                for (int i = 0; i < filasVacias; i++)
                {
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                }
            });
        }

        private void EstadoResponsableComentarios(
    IContainer container,
    EventoMetrologico evento,
    ResponsableInterno responsable)
        {
            var estado = evento.EstadoEquipoResultado ?? "";
            var estadoNormalizado = estado.ToLower();

            var colorEstado = estadoNormalizado.Contains("operativo") && !estadoNormalizado.Contains("no operativo")
                ? "#2E7D32"
                : "#C62828";

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell()
                    .Border(0.5f)
                    .Padding(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("ESTADO DEL EQUIPO:\n(Operativo; No Operativo; Fuera de Servicio)")
                    .Bold()
                    .FontSize(8);

                table.Cell()
                    .Border(0.5f)
                    .Background("#F9F9F9")
                    .Padding(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(estado)
                    .Bold()
                    .FontSize(11)
                    .FontColor(colorEstado);

                table.Cell()
                    .ColumnSpan(2)
                    .Border(0.5f)
                    .Padding(8)
                    .Column(col =>
                    {
                        col.Item().Text("Responsable Interno:").Bold().FontSize(8);
                        col.Item().PaddingTop(4).Text($"Nombre: {responsable.NombreCompleto}").FontSize(8);
                        col.Item().PaddingTop(10).Text("Firma:").FontSize(8);
                        col.Item().PaddingTop(4).LineHorizontal(0.8f);
                    });

                table.Cell()
                    .ColumnSpan(2)
                    .Border(0.5f)
                    .Padding(6)
                    .Column(col =>
                    {
                        col.Item()
                            .Text($"Comentarios Adicionales: {evento.ComentariosAdicionales ?? ""}")
                            .FontSize(8);

                        col.Item().PaddingTop(6).LineHorizontal(0.5f);
                        col.Item().PaddingTop(8).LineHorizontal(0.5f);
                    });
            });
        }

        private void Firmas(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell()
                    .Border(0.5f)
                    .Padding(8)
                    .Column(col =>
                    {
                        col.Item().AlignCenter().Text("Elaborado por:").FontSize(8);
                        col.Item().PaddingTop(18).LineHorizontal(0.8f);
                    });

                table.Cell()
                    .Border(0.5f)
                    .Padding(8)
                    .Column(col =>
                    {
                        col.Item().AlignCenter().Text("Aprobado por:").FontSize(8);
                        col.Item().PaddingTop(18).LineHorizontal(0.8f);
                    });
            });
        }

        private static void Campo(ColumnDescriptor col, string etiqueta, string? valor)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text(etiqueta).Bold().FontSize(8);
                row.RelativeItem(4).Text(valor ?? "***").FontSize(8);
            });
        }

        private static string ObtenerCaracteristica(Equipo equipo, string nombre)
        {
            return equipo.CaracteristicasMetrologicas?
                .FirstOrDefault(c => c.Nombre.ToLower().Contains(nombre.ToLower()))
                ?.Valor ?? "***";
        }

        private static void HeaderCell(TableDescriptor table, string texto)
        {
            table.Cell()
                .Border(0.5f)
                .Background("#EAEAEA")
                .PaddingVertical(6)
                .PaddingHorizontal(4)
                .AlignCenter()
                .AlignMiddle()
                .Text(texto)
                .Bold()
                .FontSize(9);
        }

        private static void FilaMeta(TableDescriptor table, string etiqueta, string valor)
        {
            table.Cell().Border(0.5f).Padding(3).Text(etiqueta).Bold().FontSize(8);
            table.Cell().Border(0.5f).Padding(3).Text(valor).FontSize(8);
        }
    }
}