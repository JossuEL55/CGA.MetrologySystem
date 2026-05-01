using CGA.MetrologySystem.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CGA.MetrologySystem.Services.Pdf
{
    public class MantenimientoPdfService
    {
        public byte[] Generar(EventoMantenimientoDato mantenimiento)
        {
            var evento = mantenimiento.EventoMetrologico;
            var equipo = evento.Equipo;
            var responsable = evento.ResponsableInterno;
            var actividades = evento.ActividadesMantenimiento
                .OrderBy(a => a.Orden)
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
                        column.Item().Element(container => Encabezado(container));
                        column.Item().Element(container => DatosEquipo(container, equipo));
                        column.Item().Element(container => TipoYFechas(container, mantenimiento, evento));
                        column.Item().Element(container => TablaActividades(container, actividades, responsable));
                        column.Item().Element(container => EstadoYComentarios(container, evento));
                        column.Item().Element(container => Firmas(container, evento));
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

                table.Cell().Border(1).Height(65).AlignCenter().AlignMiddle().Text("CGA").Bold().FontSize(16);

                table.Cell().Border(1).Column(col =>
                {
                    col.Item().AlignCenter().Text("NTE INEN ISO/IEC 17020").Bold().FontSize(9);
                    col.Item().PaddingTop(12).AlignCenter().Text("MANTENIMIENTO DE EQUIPOS").Bold().FontSize(18);
                });

                table.Cell().Border(1).Table(inner =>
                {
                    inner.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    FilaMeta(inner, "Código:", "CGA-FOR-023");
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

                table.Cell().ColumnSpan(2).Border(1).AlignCenter().Text("DATOS DEL EQUIPO").Bold();

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

        private void TipoYFechas(IContainer container, EventoMantenimientoDato mantenimiento, EventoMetrologico evento)
        {
            var esPreventivo = mantenimiento.TipoMantenimiento.Nombre.Equals("Preventivo", StringComparison.OrdinalIgnoreCase);
            var esCorrectivo = mantenimiento.TipoMantenimiento.Nombre.Equals("Correctivo", StringComparison.OrdinalIgnoreCase);

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).AlignCenter().AlignMiddle().Text("TIPO DE MANTENIMIENTO").Bold();

                table.Cell().Border(1).Padding(4).Column(col =>
                {
                    col.Item().Text($"{(esPreventivo ? "X" : "□")} PREVENTIVO").Bold();
                    col.Item().Text($"{(esCorrectivo ? "X" : "□")} CORRECTIVO").Bold();
                });

                table.Cell().Border(1).AlignCenter().Text($"FECHA: {evento.FechaEvento:yyyy-MM-dd}").Bold();

                table.Cell().Border(1).AlignCenter().Text("FRECUENCIA DE MANTENIMIENTO: 4 MESES").Bold();
            });
        }

        private void TablaActividades(IContainer container, List<EventoMantenimientoActividad> actividades, ResponsableInterno responsable)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(5);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                });

                HeaderCell(table, "Actividades realizadas");
                HeaderCell(table, "Responsable Interno");
                HeaderCell(table, "Observaciones");

                foreach (var actividad in actividades)
                {
                    table.Cell().Border(1).Padding(4).MinHeight(35).Text(actividad.DescripcionActividad);
                    table.Cell().Border(1).Padding(4).AlignCenter().AlignMiddle().Text(responsable.NombreCompleto);
                    table.Cell().Border(1).Padding(4).Text(actividad.Observaciones ?? "");
                }

                var filasVacias = Math.Max(0, 7 - actividades.Count);

                for (int i = 0; i < filasVacias; i++)
                {
                    table.Cell().Border(1).MinHeight(28).Text("");
                    table.Cell().Border(1).Text("");
                    table.Cell().Border(1).Text("");
                }
            });
        }

        private void EstadoYComentarios(IContainer container, EventoMetrologico evento)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).Padding(8).AlignCenter().Text("ESTADO DEL EQUIPO:\n(Operativo;No Operativo;Fuera de Servicio)").Bold();
                table.Cell().Border(1).Padding(8).AlignCenter().AlignMiddle().Text(evento.EstadoEquipoResultado ?? "").Bold();

                table.Cell().ColumnSpan(2).Border(1).Padding(4)
                    .Text($"Comentarios Adicionales: {evento.ComentariosAdicionales ?? ""}");
            });
        }

        private void Firmas(IContainer container, EventoMetrologico evento)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).Padding(8).AlignCenter()
                    .Text($"Próxima fecha de mantenimiento:\n{(evento.FechaProxima.HasValue ? evento.FechaProxima.Value.ToString("yyyy-MM-dd") : "N/D")}");

                table.Cell().Border(1).Padding(8).AlignCenter()
                    .Text("Elaborado por:\n\n_______________________");

                table.Cell().Border(1).Padding(8).AlignCenter()
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
            table.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(texto).Bold();
        }

        private static void FilaMeta(TableDescriptor table, string etiqueta, string valor)
        {
            table.Cell().Border(1).Padding(2).Text(etiqueta).Bold();
            table.Cell().Border(1).Padding(2).Text(valor);
        }
    }
}