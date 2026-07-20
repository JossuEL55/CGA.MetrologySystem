using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Services.Pdf;
using QuestPDF.Infrastructure;
using Xunit;

namespace CGA.MetrologySystem.Tests;

[CollectionDefinition("PDF generation", DisableParallelization = true)]
public sealed class PdfGenerationCollection;

[Collection("PDF generation")]
public class PdfServicesTests
{
    [Fact]
    public void FichaTecnicaPdfService_EquipoCompletoYTodosLosEventos_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var (equipo, eventos) = CrearEscenarioCompleto();

            var pdf = new FichaTecnicaPdfService().Generar(equipo, eventos);

            VerificarPdf(pdf);
        });
    }

    [Fact]
    public void FichaTecnicaPdfService_EquipoMinimoSinEventos_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var pdf = new FichaTecnicaPdfService().Generar(CrearEquipoMinimo(), new List<EventoMetrologico>());

            VerificarPdf(pdf);
        });
    }

    [Fact]
    public void HojaVidaPdfService_EquipoCompletoYEventosDesordenados_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var (equipo, eventos) = CrearEscenarioCompleto();
            eventos.Reverse();

            var pdf = new HojaVidaPdfService().Generar(equipo, eventos);

            VerificarPdf(pdf);
        });
    }

    [Fact]
    public void HojaVidaPdfService_EquipoSinProveedorCaracteristicasNiEventos_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var pdf = new HojaVidaPdfService().Generar(CrearEquipoMinimo(), new List<EventoMetrologico>());

            VerificarPdf(pdf);
        });
    }

    [Fact]
    public void VerificacionPdfService_ResultadosCumpleYNoCumple_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var (equipo, eventos) = CrearEscenarioCompleto();
            var evento = eventos.Single(e => e.TipoEventoMetrologico.Nombre == "Verificación");
            var verificacion = evento.EventoVerificacionDato!;

            var pdf = new VerificacionPdfService().Generar(verificacion);

            VerificarPdf(pdf);
        });
    }

    [Fact]
    public void VerificacionPdfService_SinResultadosNiDatosOpcionales_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var evento = CrearEventoMinimo("Verificación");
            evento.EventoVerificacionDato = new EventoVerificacionDato { EventoMetrologico = evento };

            var pdf = new VerificacionPdfService().Generar(evento.EventoVerificacionDato);

            VerificarPdf(pdf);
        });
    }

    [Fact]
    public void MantenimientoPdfService_ActividadesConObservaciones_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var (_, eventos) = CrearEscenarioCompleto();
            var evento = eventos.Single(e => e.TipoEventoMetrologico.Nombre == "Mantenimiento");
            var mantenimiento = evento.EventoMantenimientoDato!;

            var pdf = new MantenimientoPdfService().Generar(mantenimiento);

            VerificarPdf(pdf);
        });
    }

    [Fact]
    public void MantenimientoPdfService_SinActividadesNiComentarios_GeneraPdfValido()
    {
        EjecutarDesdeProyecto(() =>
        {
            var evento = CrearEventoMinimo("Mantenimiento");
            evento.EventoMantenimientoDato = new EventoMantenimientoDato
            {
                EventoMetrologico = evento,
                TipoMantenimiento = new TipoMantenimiento { Nombre = "Preventivo" }
            };

            var pdf = new MantenimientoPdfService().Generar(evento.EventoMantenimientoDato);

            VerificarPdf(pdf);
        });
    }

    private static void EjecutarDesdeProyecto(Action action)
    {
        var directorioOriginal = Directory.GetCurrentDirectory();
        var directorioProyecto = EncontrarDirectorioProyecto();

        Assert.True(File.Exists(Path.Combine(directorioProyecto, "wwwroot", "images", "logo.png")));

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            Directory.SetCurrentDirectory(directorioProyecto);
            action();
        }
        finally
        {
            Directory.SetCurrentDirectory(directorioOriginal);
        }
    }

    private static string EncontrarDirectorioProyecto()
    {
        DirectoryInfo? actual = new(AppContext.BaseDirectory);

        while (actual != null)
        {
            var candidato = Path.Combine(actual.FullName, "CGA.MetrologySystem");
            if (File.Exists(Path.Combine(candidato, "CGA.MetrologySystem.csproj")))
            {
                return candidato;
            }

            actual = actual.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró el proyecto web para resolver sus recursos PDF.");
    }

    private static void VerificarPdf(byte[] contenido)
    {
        Assert.True(contenido.Length > 1_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(contenido, 0, 4));
        Assert.Contains("%%EOF", System.Text.Encoding.ASCII.GetString(contenido[^32..]));
    }

    private static (Equipo Equipo, List<EventoMetrologico> Eventos) CrearEscenarioCompleto()
    {
        var responsable = new ResponsableInterno
        {
            ResponsableInternoId = 1,
            NombreCompleto = "Responsable Metrológico",
            Cargo = "Inspector",
            Correo = "responsable@example.com",
            Telefono = "0999999999"
        };
        var equipo = new Equipo
        {
            EquipoId = 1,
            Codigo = "CGA-PDF-001",
            Nombre = "Balanza analítica",
            Marca = "Marca de prueba",
            Modelo = "Modelo X",
            Serie = "SER-123",
            Identificacion = "ID-001",
            FechaAdquisicion = new DateTime(2024, 1, 10),
            FechaPuestaFuncionamiento = new DateTime(2024, 2, 1),
            FabricanteLugarOrigen = "Fabricante - Ecuador",
            CatalogoManejoOperacion = "CAT-001",
            MantenimientoFabricante = "Anual",
            CondicionesOperacion = "20 °C ± 2 °C",
            TipoEquipo = new TipoEquipo { Nombre = "Masa" },
            Proveedor = new Proveedor
            {
                Nombre = "Proveedor técnico",
                Direccion = "Quito",
                Email = "proveedor@example.com",
                Telefono = "022222222"
            },
            Ubicacion = new Ubicacion { Nombre = "Laboratorio principal" },
            ResponsableInterno = responsable
        };

        foreach (var (nombre, valor, unidad, orden) in new[]
        {
            ("Rango", "0 - 200", "g", 1),
            ("Medición", "Masa", "g", 2),
            ("Exactitud", "0.001", "g", 3),
            ("Patrones", "Pesas clase E2", "", 4),
            ("Mantenimiento", "Limpieza mensual", "", 5)
        })
        {
            equipo.CaracteristicasMetrologicas.Add(new CaracteristicaMetrologicaEquipo
            {
                Nombre = nombre,
                Valor = valor,
                Unidad = unidad,
                Orden = orden,
                Equipo = equipo
            });
        }

        var tipoCalibracion = new TipoEventoMetrologico { Nombre = "Calibración" };
        var tipoVerificacion = new TipoEventoMetrologico { Nombre = "Verificación" };
        var tipoMantenimiento = new TipoEventoMetrologico { Nombre = "Mantenimiento" };
        equipo.ConfiguracionesControl.Add(new ConfiguracionControlEquipo
        {
            Equipo = equipo,
            TipoEventoMetrologico = tipoCalibracion,
            PeriodicidadValor = 12,
            PeriodicidadUnidad = "meses",
            Activo = true
        });
        equipo.ConfiguracionesControl.Add(new ConfiguracionControlEquipo
        {
            Equipo = equipo,
            TipoEventoMetrologico = tipoVerificacion,
            PeriodicidadValor = 6,
            PeriodicidadUnidad = "meses",
            Activo = true
        });

        var calibracion = CrearEvento(equipo, responsable, tipoCalibracion, new DateTime(2025, 1, 10));
        calibracion.ComentariosAdicionales = "Calibración satisfactoria";
        calibracion.EventoCalibracionDato = new EventoCalibracionDato
        {
            EventoMetrologico = calibracion,
            NumeroCertificado = "CERT-001",
            FechaCalibracion = calibracion.FechaEvento,
            Laboratorio = new Laboratorio { Nombre = "Laboratorio acreditado" },
            Observaciones = "Sin novedades"
        };

        var verificacion = CrearEvento(equipo, responsable, tipoVerificacion, new DateTime(2025, 7, 10));
        verificacion.EstadoEquipoResultado = "Operativo";
        verificacion.ComentariosAdicionales = "Verificación dentro de tolerancia";
        verificacion.EventoVerificacionDato = new EventoVerificacionDato { EventoMetrologico = verificacion };
        verificacion.ResultadosVerificacion.Add(new EventoVerificacionResultado
        {
            DescripcionItem = "Inspección visual",
            Cumple = true,
            Observaciones = "Correcto",
            Orden = 1,
            EventoMetrologico = verificacion
        });
        verificacion.ResultadosVerificacion.Add(new EventoVerificacionResultado
        {
            DescripcionItem = "Prueba funcional",
            Cumple = false,
            Observaciones = "Requiere ajuste",
            Orden = 2,
            EventoMetrologico = verificacion
        });

        var mantenimiento = CrearEvento(equipo, responsable, tipoMantenimiento, new DateTime(2025, 10, 10));
        mantenimiento.EstadoEquipoResultado = "Disponible";
        mantenimiento.ComentariosAdicionales = "Mantenimiento terminado";
        mantenimiento.EventoMantenimientoDato = new EventoMantenimientoDato
        {
            EventoMetrologico = mantenimiento,
            TipoMantenimiento = new TipoMantenimiento { Nombre = "Preventivo" }
        };
        mantenimiento.ActividadesMantenimiento.Add(new EventoMantenimientoActividad
        {
            DescripcionActividad = "Limpieza general",
            Observaciones = "Sin residuos",
            Orden = 1,
            EventoMetrologico = mantenimiento
        });
        mantenimiento.ActividadesMantenimiento.Add(new EventoMantenimientoActividad
        {
            DescripcionActividad = "Ajuste mecánico",
            Observaciones = null,
            Orden = 2,
            EventoMetrologico = mantenimiento
        });

        var otro = CrearEvento(
            equipo,
            responsable,
            new TipoEventoMetrologico { Nombre = "Inspección especial" },
            new DateTime(2025, 11, 1));
        otro.ComentariosAdicionales = "Actividad adicional";

        return (equipo, new List<EventoMetrologico> { calibracion, verificacion, mantenimiento, otro });
    }

    private static Equipo CrearEquipoMinimo()
    {
        return new Equipo
        {
            Codigo = "MIN-001",
            Nombre = "Equipo mínimo",
            TipoEquipo = new TipoEquipo { Nombre = "Sin clasificar" },
            Proveedor = null!,
            Ubicacion = null!,
            ResponsableInterno = null!
        };
    }

    private static EventoMetrologico CrearEventoMinimo(string tipo)
    {
        var equipo = CrearEquipoMinimo();
        var responsable = new ResponsableInterno { NombreCompleto = "Responsable" };
        return CrearEvento(
            equipo,
            responsable,
            new TipoEventoMetrologico { Nombre = tipo },
            DateTime.Today);
    }

    private static EventoMetrologico CrearEvento(
        Equipo equipo,
        ResponsableInterno responsable,
        TipoEventoMetrologico tipo,
        DateTime fecha)
    {
        return new EventoMetrologico
        {
            Equipo = equipo,
            EquipoId = equipo.EquipoId,
            TipoEventoMetrologico = tipo,
            ResponsableInterno = responsable,
            FechaEvento = fecha,
            FechaProxima = fecha.AddMonths(6),
            Activo = true
        };
    }
}
