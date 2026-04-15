using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities

{

    public class Laboratorio

    {

        public int LaboratorioId { get; set; }



        public string Nombre { get; set; } = null!;



        public string? Direccion { get; set; }

        public string? Ciudad { get; set; }

        public string? Pais { get; set; }



        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? SitioWeb { get; set; }



        public string? NormaAcreditacion { get; set; }



        public bool Activo { get; set; } = true;



        public ICollection<EventoCalibracionDato> EventosCalibracion { get; set; } = new List<EventoCalibracionDato>();

    }

}