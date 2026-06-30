using CGA.MetrologySystem.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CGA.MetrologySystem.Services.Pdf
{
    public class HojaVidaPdfService
    {
        public byte[] Generar(Equipo equipo, List<EventoMetrologico> eventos)
        {
            var eventosOrdenados = eventos
                .OrderBy(e => e.FechaEvento)
                .ThenBy(e => e.EventoMetrologicoId)
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
                        column.Item().Element(container => IdentificacionEquipo(container, equipo));
                        column.Item().Element(container => DatosProveedor(container, equipo));
                        column.Item().Element(container => CaracteristicasMetrologicas(container, equipo));
                        column.Item().Element(container => ControlActividades(container, eventosOrdenados));
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
                    .Height(78)
                    .Padding(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Image(logoPath)
                    .FitArea();

                table.Cell()
                    .Border(0.5f)
                    .Height(78)
                    .AlignCenter()
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().AlignCenter().Text("HOJA DE VIDA DE EQUIPO")
                            .FontSize(17)
                            .Bold();

                        col.Item().PaddingTop(5).AlignCenter().Text("NTE INEN ISO/IEC 17020")
                            .FontSize(8)
                            .Italic();
                    });

                table.Cell()
                    .Border(0.5f)
                    .Height(78)
                    .Table(inner =>
                    {
                        inner.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        FilaMeta(inner, "Código:", "CGA-FOR-015");
                        FilaMeta(inner, "Fecha:", "2024-08-15");
                        FilaMeta(inner, "Revisión:", "Original");
                        FilaMeta(inner, "Página:", "1 de 3");
                    });
            });
        }

        private void IdentificacionEquipo(IContainer container, Equipo equipo)
        {
            container.PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                SeccionTitulo(table, "IDENTIFICACIÓN Y ESPECIFICACIONES DEL EQUIPO", 2);

                table.Cell().Border(0.5f).Padding(6).Column(col =>
                {
                    Campo(col, "Nombre del equipo:", equipo.Nombre);
                    Campo(col, "Ubicación del equipo:", equipo.Ubicacion?.Nombre);
                    Campo(col, "Marca:", equipo.Marca);
                    Campo(col, "Modelo:", equipo.Modelo);
                });

                table.Cell().Border(0.5f).Padding(6).Column(col =>
                {
                    Campo(col, "Identificación:", equipo.Codigo);
                    Campo(col, "Serie:", equipo.Serie);
                    Campo(col, "Fecha de adquisición:", equipo.FechaAdquisicion?.ToString("yyyy-MM-dd"));
                    Campo(col, "Fecha de puesta en funcionamiento:", equipo.FechaPuestaFuncionamiento?.ToString("yyyy-MM-dd"));
                });

                table.Cell().ColumnSpan(2).Border(0.5f).Padding(6).Column(col =>
                {
                    Campo(col, "Fabricante-Lugar de origen:", equipo.FabricanteLugarOrigen);
                    Campo(col, "Catálogo de manejo u operación:", ObtenerCatalogo(equipo));
                    Campo(col, "Condiciones de operación:", equipo.CondicionesOperacion);
                });
            });
        }

        private void DatosProveedor(IContainer container, Equipo equipo)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                });

                SeccionTitulo(table, "DATOS DEL PROVEEDOR", 1);

                table.Cell().Border(0.5f).Padding(6).Column(col =>
                {
                    Campo(col, "Nombre de proveedor-Dirección:", ObtenerProveedorDireccion(equipo));
                    Campo(col, "Email-Tlf:", ObtenerProveedorContacto(equipo));
                });
            });
        }

        private void CaracteristicasMetrologicas(IContainer container, Equipo equipo)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                SeccionTitulo(table, "CARACTERÍSTICAS METROLÓGICAS DEL EQUIPO", 2);

                table.Cell().Border(0.5f).Padding(6).Column(col =>
                {
                    Campo(col, "Rango:", ObtenerCaracteristica(equipo, "Rango"));
                    Campo(col, "Medición a realizar:", ObtenerCaracteristica(equipo, "Medición"));
                    Campo(col, "Exactitud:", ObtenerCaracteristica(equipo, "Exactitud"));
                });

                table.Cell().Border(0.5f).Padding(6).Column(col =>
                {
                    Campo(col, "Frecuencia de calibración:", ObtenerFrecuencia(equipo, "Calibración"));
                    Campo(col, "Frecuencia de verificación:", ObtenerFrecuencia(equipo, "Verificación"));
                    Campo(col, "Patrones utilizados:", ObtenerCaracteristica(equipo, "Patrones"));
                    Campo(col, "Mantenimiento indicado por el fabricante:", ObtenerCaracteristica(equipo, "Mantenimiento"));
                });
            });
        }

        private void ControlActividades(IContainer container, List<EventoMetrologico> eventos)
        {
            container.PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(0.6f);
                    columns.RelativeColumn(0.6f);
                    columns.RelativeColumn(0.6f);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(3);
                });

                table.Cell().ColumnSpan(6)
                    .Border(0.5f)
                    .Background("#E0E0E0")
                    .PaddingVertical(4)
                    .AlignCenter()
                    .Text("CONTROL DE ACTIVIDADES")
                    .Bold()
                    .FontSize(9);

                table.Cell().ColumnSpan(6)
                    .Border(0.5f)
                    .Padding(4)
                    .AlignCenter()
                    .Text("Calibración: C       Verificación: V       Mantenimiento: M")
                    .FontSize(8);

                HeaderCell(table, "Fecha");
                HeaderCell(table, "C");
                HeaderCell(table, "V");
                HeaderCell(table, "M");
                HeaderCell(table, "Descripción");
                HeaderCell(table, "Responsable");

                foreach (var evento in eventos)
                {
                    table.Cell().Border(0.5f).Padding(4).AlignMiddle()
                        .Text(evento.FechaEvento.ToString("yyyy-MM-dd"))
                        .FontSize(7);

                    table.Cell().Border(0.5f).Padding(4).AlignCenter().AlignMiddle()
                        .Text(EsTipo(evento, "calibr") ? "X" : "")
                        .Bold()
                        .FontSize(8);

                    table.Cell().Border(0.5f).Padding(4).AlignCenter().AlignMiddle()
                        .Text(EsTipo(evento, "verific") ? "X" : "")
                        .Bold()
                        .FontSize(8);

                    table.Cell().Border(0.5f).Padding(4).AlignCenter().AlignMiddle()
                        .Text(EsTipo(evento, "manten") ? "X" : "")
                        .Bold()
                        .FontSize(8);

                    table.Cell().Border(0.5f).Padding(4).AlignMiddle()
                        .Text(ObtenerDescripcionEvento(evento))
                        .FontSize(7);

                    table.Cell().Border(0.5f).Padding(4).AlignMiddle()
                        .Text(evento.ResponsableInterno?.NombreCompleto ?? "***")
                        .FontSize(7);
                }

                var filasVacias = Math.Max(0, 6 - eventos.Count);

                for (int i = 0; i < filasVacias; i++)
                {
                    table.Cell().Border(0.5f).MinHeight(24).Text("");
                    table.Cell().Border(0.5f).MinHeight(24).Text("");
                    table.Cell().Border(0.5f).MinHeight(24).Text("");
                    table.Cell().Border(0.5f).MinHeight(24).Text("");
                    table.Cell().Border(0.5f).MinHeight(24).Text("");
                    table.Cell().Border(0.5f).MinHeight(24).Text("");
                }
            });
        }

        private void Firmas(IContainer container)
        {
            container.PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(0.5f).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("Elaborado por:").FontSize(8);
                    col.Item().PaddingTop(18).LineHorizontal(0.8f);
                });

                table.Cell().Border(0.5f).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("Revisado por:").FontSize(8);
                    col.Item().PaddingTop(18).LineHorizontal(0.8f);
                });
            });
        }

        private static bool EsTipo(EventoMetrologico evento, string texto)
        {
            return evento.TipoEventoMetrologico?.Nombre?
                .ToLower()
                .Contains(texto) == true;
        }

        private static string ObtenerDescripcionEvento(EventoMetrologico evento)
        {
            if (EsTipo(evento, "calibr") && EsTipo(evento, "verific"))
                return "Calibración/Verificación de Equipo";

            if (EsTipo(evento, "calibr"))
                return "Calibración de Equipo";

            if (EsTipo(evento, "verific"))
                return "Verificación de Equipo";

            if (EsTipo(evento, "manten"))
                return "Mantenimiento de Equipo";

            return evento.TipoEventoMetrologico?.Nombre ?? "***";
        }

        private static string ObtenerCatalogo(Equipo equipo)
        {
            return string.IsNullOrWhiteSpace(equipo.CatalogoManejoOperacion)
                ? "***"
                : equipo.CatalogoManejoOperacion;
        }

        private static string ObtenerProveedorDireccion(Equipo equipo)
        {
            if (equipo.Proveedor == null)
                return "***";

            if (!string.IsNullOrWhiteSpace(equipo.Proveedor.Direccion))
                return $"{equipo.Proveedor.Nombre} - {equipo.Proveedor.Direccion}";

            return equipo.Proveedor.Nombre;
        }

        private static string ObtenerProveedorContacto(Equipo equipo)
        {
            if (equipo.Proveedor == null)
                return "***";

            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(equipo.Proveedor.Email))
                partes.Add(equipo.Proveedor.Email);

            if (!string.IsNullOrWhiteSpace(equipo.Proveedor.Telefono))
                partes.Add(equipo.Proveedor.Telefono);

            return partes.Any() ? string.Join(" - ", partes) : "***";
        }

        private static string ObtenerCaracteristica(Equipo equipo, string nombre)
        {
            return equipo.CaracteristicasMetrologicas?
                .FirstOrDefault(c => c.Nombre.ToLower().Contains(nombre.ToLower()))
                ?.Valor ?? "***";
        }

        private static string ObtenerFrecuencia(Equipo equipo, string tipoEvento)
        {
            var configuracion = equipo.ConfiguracionesControl?
                .FirstOrDefault(c =>
                    c.Activo &&
                    c.TipoEventoMetrologico != null &&
                    c.TipoEventoMetrologico.Nombre.ToLower().Contains(tipoEvento.ToLower()));

            if (configuracion == null)
                return "***";

            if (configuracion.PeriodicidadValor.HasValue &&
                !string.IsNullOrWhiteSpace(configuracion.PeriodicidadUnidad))
            {
                return $"{configuracion.PeriodicidadValor} {configuracion.PeriodicidadUnidad}";
            }

            return "***";
        }

        private static void Campo(ColumnDescriptor col, string etiqueta, string? valor)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2.2f).Text(etiqueta).Bold().FontSize(8);
                row.RelativeItem(4).Text(valor ?? "***").FontSize(8);
            });
        }

        private static void HeaderCell(TableDescriptor table, string texto)
        {
            table.Cell()
                .Border(0.5f)
                .Background("#EAEAEA")
                .Padding(4)
                .AlignCenter()
                .AlignMiddle()
                .Text(texto)
                .Bold()
                .FontSize(8);
        }

        private static void SeccionTitulo(TableDescriptor table, string texto, uint columnas)
        {
            table.Cell()
                .ColumnSpan(columnas)
                .Border(0.5f)
                .Background("#E0E0E0")
                .PaddingVertical(4)
                .AlignCenter()
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