using CGA.MetrologySystem.Models.MaestroEquipos;
using ClosedXML.Excel;

namespace CGA.MetrologySystem.Services.MaestroEquipos
{
    public class MaestroEquiposExcelService
    {
        private const string HojaListado = "Listado Maestro";

        private readonly MaestroEquiposService _maestroEquiposService;

        public MaestroEquiposExcelService(MaestroEquiposService maestroEquiposService)
        {
            _maestroEquiposService = maestroEquiposService;
        }

        public async Task<MaestroEquiposExcelResult> GenerarListadoMaestroAsync(
            MaestroEquiposFiltroViewModel filtros,
            string? usuarioExportador)
        {
            var listado = await _maestroEquiposService.ObtenerIndexAsync(filtros);
            var fechaExportacion = DateTime.Now;

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(HojaListado);

            EscribirEncabezados(worksheet);
            EscribirEquipos(
                worksheet,
                listado.Equipos,
                fechaExportacion,
                usuarioExportador,
                ConstruirDescripcionFiltros(listado.Filtros));
            AplicarFormato(worksheet, listado.Equipos.Count);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new MaestroEquiposExcelResult
            {
                Contenido = stream.ToArray(),
                NombreArchivo = $"ListadoMaestroEquipos_{fechaExportacion:yyyyMMdd_HHmm}.xlsx"
            };
        }

        private static void EscribirEncabezados(IXLWorksheet worksheet)
        {
            var encabezados = new[]
            {
                "Codigo equipo",
                "Nombre equipo",
                "Tipo equipo",
                "Estado global metrologico",
                "Score maximo",
                "Prioridad maxima",
                "Configuracion incompleta",
                "Estado calibracion",
                "Ultima calibracion",
                "Proxima calibracion",
                "Dias calibracion",
                "Mensaje calibracion",
                "Estado verificacion",
                "Ultima verificacion",
                "Proxima verificacion",
                "Dias verificacion",
                "Mensaje verificacion",
                "Estado mantenimiento",
                "Ultimo mantenimiento",
                "Proximo mantenimiento",
                "Dias mantenimiento",
                "Mensaje mantenimiento",
                "Fecha de exportacion",
                "Usuario exportador",
                "Filtros aplicados"
            };

            for (var i = 0; i < encabezados.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = encabezados[i];
            }
        }

        private static void EscribirEquipos(
            IXLWorksheet worksheet,
            IReadOnlyList<MaestroEquipoItemViewModel> equipos,
            DateTime fechaExportacion,
            string? usuarioExportador,
            string filtrosAplicados)
        {
            var fila = 2;

            foreach (var equipo in equipos)
            {
                var calibracion = ObtenerControl(equipo, "calibr");
                var verificacion = ObtenerControl(equipo, "verific");
                var mantenimiento = ObtenerControl(equipo, "manten");

                worksheet.Cell(fila, 1).Value = ValorTexto(equipo.CodigoEquipo);
                worksheet.Cell(fila, 2).Value = ValorTexto(equipo.NombreEquipo);
                worksheet.Cell(fila, 3).Value = ValorTexto(equipo.TipoEquipo);
                worksheet.Cell(fila, 4).Value = ValorTexto(equipo.EstadoGlobalTexto);
                worksheet.Cell(fila, 5).Value = equipo.ScoreMaximo.HasValue ? equipo.ScoreMaximo.Value : "-";
                worksheet.Cell(fila, 6).Value = ValorTexto(equipo.PrioridadMaxima);
                worksheet.Cell(fila, 7).Value = equipo.TieneConfiguracionIncompleta ? "Si" : "No";

                EscribirControl(worksheet, fila, 8, calibracion);
                EscribirControl(worksheet, fila, 13, verificacion);
                EscribirControl(worksheet, fila, 18, mantenimiento);

                worksheet.Cell(fila, 23).Value = fechaExportacion;
                worksheet.Cell(fila, 23).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                worksheet.Cell(fila, 24).Value = ValorTexto(usuarioExportador);
                worksheet.Cell(fila, 25).Value = filtrosAplicados;

                fila++;
            }
        }

        private static void EscribirControl(
            IXLWorksheet worksheet,
            int fila,
            int columnaInicial,
            MaestroEquipoControlItemViewModel? control)
        {
            worksheet.Cell(fila, columnaInicial).Value = ValorTexto(control?.EstadoTexto);
            EscribirFecha(worksheet.Cell(fila, columnaInicial + 1), control?.FechaUltimoEvento);
            EscribirFecha(worksheet.Cell(fila, columnaInicial + 2), control?.FechaProxima);
            worksheet.Cell(fila, columnaInicial + 3).Value = control?.DiasRestantes.HasValue == true
                ? control.DiasRestantes.Value
                : "-";
            worksheet.Cell(fila, columnaInicial + 4).Value = ValorTexto(control?.Mensaje);
        }

        private static void EscribirFecha(IXLCell cell, DateTime? fecha)
        {
            if (fecha.HasValue)
            {
                cell.Value = fecha.Value;
                cell.Style.DateFormat.Format = "yyyy-MM-dd";
                return;
            }

            cell.Value = "-";
        }

        private static MaestroEquipoControlItemViewModel? ObtenerControl(
            MaestroEquipoItemViewModel equipo,
            string textoTipo)
        {
            return equipo.Controles.FirstOrDefault(c =>
                c.TipoControl.Contains(textoTipo, StringComparison.OrdinalIgnoreCase));
        }

        private static void AplicarFormato(IXLWorksheet worksheet, int totalEquipos)
        {
            const int totalColumnas = 25;
            var ultimaFila = Math.Max(totalEquipos + 1, 1);

            var rangoEncabezado = worksheet.Range(1, 1, 1, totalColumnas);
            rangoEncabezado.Style.Font.Bold = true;
            rangoEncabezado.Style.Font.FontColor = XLColor.White;
            rangoEncabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f4e79");
            rangoEncabezado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Range(1, 1, ultimaFila, totalColumnas).SetAutoFilter();
            worksheet.SheetView.FreezeRows(1);

            worksheet.Columns().AdjustToContents();
            worksheet.Columns(1, totalColumnas).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            worksheet.Column(12).Width = 38;
            worksheet.Column(17).Width = 38;
            worksheet.Column(22).Width = 38;
            worksheet.Column(25).Width = 45;
        }

        private static string ConstruirDescripcionFiltros(MaestroEquiposFiltroViewModel filtros)
        {
            var partes = new List<string>
            {
                $"Busqueda: {ValorTexto(filtros.Buscar)}",
                $"Tipo equipo: {ObtenerTextoSeleccionado(filtros.TiposEquipo, filtros.TipoEquipoId?.ToString())}",
                $"Estado global: {ObtenerTextoSeleccionado(filtros.EstadosGlobales, filtros.EstadoGlobal?.ToString())}",
                $"Solo configuracion incompleta: {(filtros.SoloConfiguracionIncompleta ? "Si" : "No")}",
                $"Horizonte dias: {filtros.HorizonteDias}"
            };

            return string.Join(" | ", partes);
        }

        private static string ObtenerTextoSeleccionado(IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> items, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return "Todos";
            }

            return items.FirstOrDefault(i => i.Value == valor)?.Text ?? valor;
        }

        private static string ValorTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor;
        }
    }
}
