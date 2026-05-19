using CGA.MetrologySystem.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace CGA.MetrologySystem.Services.Pdf
{
    public class FichaTecnicaPdfService
    {
        public byte[] Generar(Equipo equipo, List<EventoMetrologico> eventos)
        {
            var eventosOrdenados = eventos
                .OrderBy(e => e.FechaEvento)
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
                        column.Item().Element(container => TablaHistorial(container, eventosOrdenados));
                        column.Item().Element(Firmas);
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

                        col.Item().PaddingTop(4).AlignCenter().Text("FICHA TÉCNICA DE EQUIPOS")
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

                        FilaMeta(inner, "Código:", "CGA-FOR-023");
                        FilaMeta(inner, "Fecha:", "2024-02-15");
                        FilaMeta(inner, "Revisión:", "Original");
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

        private void TablaHistorial(IContainer container, List<EventoMetrologico> eventos)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                });

                HeaderCell(table, "Proveedor");
                HeaderCell(table, "Fecha");
                HeaderCell(table, "Responsable Interno");
                HeaderCell(table, "Actividad realizada");
                HeaderCell(table, "Resultado");
                HeaderCell(table, "Observaciones/Actividades");

                foreach (var evento in eventos)
                {
                    table.Cell().Border(0.5f).Padding(5).MinHeight(32).AlignMiddle()
                        .Text(ObtenerProveedor(evento))
                        .FontSize(7);

                    table.Cell().Border(0.5f).Padding(5).MinHeight(32).AlignMiddle()
                        .Text(evento.FechaEvento.ToString("yyyy-MM-dd"))
                        .FontSize(7);

                    table.Cell().Border(0.5f).Padding(5).MinHeight(32).AlignMiddle()
                        .Text(evento.ResponsableInterno?.NombreCompleto ?? "***")
                        .FontSize(7);

                    table.Cell().Border(0.5f).Padding(5).MinHeight(32).AlignMiddle()
                        .Text(ObtenerActividad(evento))
                        .FontSize(7);

                    table.Cell().Border(0.5f).Padding(5).MinHeight(32).AlignMiddle()
                        .Text(evento.EstadoEquipoResultado ?? "***")
                        .FontSize(7);

                    table.Cell().Border(0.5f).Padding(5).MinHeight(32).AlignMiddle()
                        .Text(ObtenerObservacion(evento))
                        .FontSize(7);
                }

                var filasVacias = Math.Max(0, 7 - eventos.Count);

                for (int i = 0; i < filasVacias; i++)
                {
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                    table.Cell().Border(0.5f).MinHeight(28).Text("");
                }
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

        private static string ObtenerProveedor(EventoMetrologico evento)
        {
            if (evento.EventoCalibracionDato?.Laboratorio != null)
                return evento.EventoCalibracionDato.Laboratorio.Nombre;

            return evento.Equipo?.Proveedor?.Nombre ?? "***";
        }

        private static string ObtenerActividad(EventoMetrologico evento)
        {
            var tipo = evento.TipoEventoMetrologico?.Nombre?.ToLower() ?? "";

            if (tipo.Contains("calibr"))
                return "Calibración";

            if (tipo.Contains("verific"))
                return "Verificación";

            if (tipo.Contains("manten"))
                return evento.EventoMantenimientoDato?.TipoMantenimiento?.Nombre != null
                    ? $"Mantenimiento {evento.EventoMantenimientoDato.TipoMantenimiento.Nombre}"
                    : "Mantenimiento";

            return evento.TipoEventoMetrologico?.Nombre ?? "***";
        }

        private static string ObtenerObservacion(EventoMetrologico evento)
        {
            var tipo = evento.TipoEventoMetrologico?.Nombre?.ToLower() ?? "";

            if (tipo.Contains("verific"))
            {
                return evento.ComentariosAdicionales
                    ?? "Se realiza una verificación, inspección visual del equipo.";
            }

            if (tipo.Contains("manten"))
            {
                var actividades = evento.ActividadesMantenimiento?
                    .OrderBy(a => a.Orden)
                    .Select(a => a.DescripcionActividad)
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .ToList() ?? new List<string>();

                if (actividades.Any())
                    return string.Join(" / ", actividades);

                return evento.ComentariosAdicionales
                    ?? "Se realiza mantenimiento del equipo.";
            }

            if (tipo.Contains("calibr"))
            {
                return evento.ComentariosAdicionales
                    ?? "Calibración de equipo.";
            }

            return evento.ComentariosAdicionales ?? "";
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
                .FontSize(8);
        }

        private static void FilaMeta(TableDescriptor table, string etiqueta, string valor)
        {
            table.Cell().Border(0.5f).Padding(3).Text(etiqueta).Bold().FontSize(8);
            table.Cell().Border(0.5f).Padding(3).Text(valor).FontSize(8);
        }
    }
}