using CGA.MetrologySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedCatalogosEquiposAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            await SeedTiposEquipoAsync(context);
            await SeedTiposEventoMetrologicoAsync(context);
            await SeedProveedoresAsync(context);
            await SeedUbicacionesAsync(context);
            await SeedResponsablesInternosAsync(context);
            await SeedSubtiposEventoAsync(context);
            await SeedTiposMantenimientoAsync(context);
        }

        private static async Task SeedTiposEquipoAsync(AppDbContext context)
        {
            async Task AgregarSiNoExiste(string nombre, string descripcion)
            {
                if (!await context.TiposEquipo.AnyAsync(x => x.Nombre == nombre))
                {
                    context.TiposEquipo.Add(new TipoEquipo
                    {
                        Nombre = nombre,
                        Descripcion = descripcion
                    });

                    await context.SaveChangesAsync();
                }
            }

            await AgregarSiNoExiste("Cinta métrica", "Instrumento dimensional tipo cinta o flexómetro para medición de longitudes.");
            await AgregarSiNoExiste("Medidor de profundidad", "Instrumento utilizado para medición de profundidad.");
            await AgregarSiNoExiste("Medidor de espesores", "Equipo utilizado para medición de espesores.");
            await AgregarSiNoExiste("Lámpara UV", "Equipo de iluminación ultravioleta utilizado en ensayos no destructivos.");
            await AgregarSiNoExiste("Medidor UV/VIS", "Equipo utilizado para medición de intensidad de luz ultravioleta o visible.");
            await AgregarSiNoExiste("Gausímetro", "Equipo utilizado para medición de campo magnético.");
            await AgregarSiNoExiste("Yoke", "Equipo de magnetización portátil utilizado en ensayos por partículas magnéticas.");
            await AgregarSiNoExiste("Bobina de magnetización", "Equipo de magnetización tipo bobina o corona utilizado en ensayos por partículas magnéticas.");
            await AgregarSiNoExiste("Probeta", "Equipo volumétrico utilizado para medición de volumen.");
            await AgregarSiNoExiste("Patrón de masa", "Patrón o accesorio de referencia de masa o peso.");
            await AgregarSiNoExiste("Patrón de espesor", "Bloque o patrón utilizado como referencia de espesor.");
            await AgregarSiNoExiste("Patrón de Ensayo No Destructivo", "Bloque o patrón utilizado para comprobaciones en ensayos no destructivos.");
        }

        private static async Task SeedTiposEventoMetrologicoAsync(AppDbContext context)
        {
            async Task AgregarSiNoExiste(string nombre)
            {
                if (!await context.TiposEventoMetrologico.AnyAsync(x => x.Nombre == nombre))
                {
                    context.TiposEventoMetrologico.Add(new TipoEventoMetrologico
                    {
                        Nombre = nombre
                    });

                    await context.SaveChangesAsync();
                }
            }

            await AgregarSiNoExiste("Calibración");
            await AgregarSiNoExiste("Verificación");
            await AgregarSiNoExiste("Mantenimiento");
        }

        private static async Task SeedProveedoresAsync(AppDbContext context)
        {
            if (!await context.Proveedores.AnyAsync())
            {
                context.Proveedores.AddRange(
                    new Proveedor { Nombre = "Edison Aillon", Direccion = "Pichincha, La Armenia Urb. La Rivera 2, La Solidaridad E3-27 y de la Paz", Email = "edison_ach@outlook.es", Telefono = "0987512281" },
                    new Proveedor { Nombre = "ACUÁTICOS EC", Direccion = "Pichincha, Quito 9 de Octubre N2684 & Santa María", Telefono = "0987054324" },
                    new Proveedor { Nombre = "GRUPO TESTEK ECUADOR" }
                );

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedUbicacionesAsync(AppDbContext context)
        {
            if (!await context.Ubicaciones.AnyAsync())
            {
                context.Ubicaciones.Add(
                    new Ubicacion
                    {
                        Nombre = "Instalaciones - CGA OIL INSPECTION SERVICES S.A.S.",
                        Descripcion = "Ubicación principal de los equipos."
                    }
                );

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedResponsablesInternosAsync(AppDbContext context)
        {
            if (!await context.ResponsablesInternos.AnyAsync())
            {
                context.ResponsablesInternos.AddRange(
                    new ResponsableInterno { NombreCompleto = "José Miguel Sandoya Mejía", Cargo = "Gerente de Operaciones" },
                    new ResponsableInterno { NombreCompleto = "José Eduardo Sandoya Mejía", Cargo = "Gerente Técnico Sustituto / Inspector nivel II" }
                );

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSubtiposEventoAsync(AppDbContext context)
        {
            if (!await context.SubtiposEvento.AnyAsync())
            {
                context.SubtiposEvento.AddRange(
                    new SubtipoEvento
                    {
                        Nombre = "Por ingreso",
                        Descripcion = "Primera calibración, verificación o mantenimiento realizado al ingresar el equipo al sistema o ponerlo en funcionamiento.",
                        Activo = true
                    },
                    new SubtipoEvento
                    {
                        Nombre = "Periódico",
                        Descripcion = "Evento realizado conforme a la frecuencia y programación establecida para el equipo.",
                        Activo = true
                    },
                    new SubtipoEvento
                    {
                        Nombre = "Extraordinario",
                        Descripcion = "Evento realizado fuera de la programación normal debido a daños, auditorías, inspecciones especiales o situaciones no planificadas.",
                        Activo = true
                    }
                );

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedTiposMantenimientoAsync(AppDbContext context)
        {
            if (!await context.TiposMantenimiento.AnyAsync())
            {
                context.TiposMantenimiento.AddRange(
                    new TipoMantenimiento
                    {
                        Nombre = "Preventivo",
                        Activo = true
                    },
                    new TipoMantenimiento
                    {
                        Nombre = "Correctivo",
                        Activo = true
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}