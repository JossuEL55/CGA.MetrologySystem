using CGA.MetrologySystem.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(6);
                    columns.RelativeColumn(2);
                });

                table.Cell().Border(1).Height(65).AlignCenter().AlignMiddle()
                    .Text("CGA").Bold().FontSize(16);

                table.Cell().Border(1).Column(col =>
                {
                    col.Item().AlignCenter().Text("NTE INEN ISO/IEC 17020").Bold().FontSize(9);
                    col.Item().PaddingTop(12).AlignCenter().Text("VERIFICACIÓN DE EQUIPOS").Bold().FontSize(16);
                });

                table.Cell().Border(1).Table(inner =>
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

                table.Cell().ColumnSpan(2).Border(1).AlignCenter()
                    .Text("DATOS DEL EQUIPO").Bold();

                table.Cell().Border(1).Padding(4).Column(col =>
                {
                    Campo(col, "Equipo:", equipo.Nombre);
                    Campo(col, "Marca:", equipo.Marca);
                    Campo(col, "Modelo:", equipo.Modelo);
                    Campo(col, "Serie:", equipo.Serie);
                });

                table.Cell().Border(1).Padding(4).Column(col =>
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

                table.Cell().Border(1).Padding(4).AlignCenter()
                    .Text($"FECHA: {evento.FechaEvento:yyyy-MM-dd}").Bold();

                table.Cell().Border(1).Padding(4).AlignCenter()
                    .Text("FRECUENCIA DE VERIFICACIÓN: 4 MESES").Bold();
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
                    table.Cell().Border(1).Padding(4).MinHeight(28)
                        .Text(resultado.DescripcionItem);

                    table.Cell().Border(1).Padding(4).AlignCenter().AlignMiddle()
                        .Text(resultado.Cumple ? "X" : "");

                    table.Cell().Border(1).Padding(4).AlignCenter().AlignMiddle()
                        .Text(!resultado.Cumple ? "X" : "");

                    table.Cell().Border(1).Padding(4)
                        .Text(resultado.Observaciones ?? "");
                }

                var filasVacias = Math.Max(0, 7 - resultados.Count);

                for (int i = 0; i < filasVacias; i++)
                {
                    table.Cell().Border(1).MinHeight(24).Text("");
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text("");
                }
            });
        }

        private void EstadoResponsableComentarios(
            IContainer container,
            EventoMetrologico evento,
            ResponsableInterno responsable)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).Padding(8).AlignCenter()
                    .Text("ESTADO DEL EQUIPO:\n(Operativo; No Operativo; Fuera de Servicio)").Bold();

                table.Cell().Border(1).Padding(8).AlignCenter().AlignMiddle()
                    .Text(evento.EstadoEquipoResultado ?? "").Bold();

                table.Cell().ColumnSpan(2).Border(1).Padding(6).Column(col =>
                {
                    col.Item().Text("Responsable Interno:").Bold();
                    col.Item().PaddingTop(4).Text($"Nombre: {responsable.NombreCompleto}");
                    col.Item().PaddingTop(8).Text("Firma: ___________________________");
                });

                table.Cell().ColumnSpan(2).Border(1).Padding(4)
                    .Text($"Comentarios Adicionales: {evento.ComentariosAdicionales ?? ""}");
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

                table.Cell().Border(1).Padding(10).AlignCenter()
                    .Text("Elaborado por:\n\n_______________________");

                table.Cell().Border(1).Padding(10).AlignCenter()
                    .Text("Aprobado por:\n\n_______________________");
            });
        }

        private static void Campo(ColumnDescriptor col, string etiqueta, string? valor)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text(etiqueta).Bold();
                row.RelativeItem(4).Text(valor ?? "***");
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
            table.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(4)
                .AlignCenter().Text(texto).Bold();
        }

        private static void FilaMeta(TableDescriptor table, string etiqueta, string valor)
        {
            table.Cell().Border(1).Padding(2).Text(etiqueta).Bold();
            table.Cell().Border(1).Padding(2).Text(valor);
        }
    }
}