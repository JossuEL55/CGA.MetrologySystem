using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class CalibracionViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un equipo.")]
        [Display(Name = "Equipo")]
        public int EquipoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un subtipo de evento.")]
        [Display(Name = "Subtipo de evento")]
        public int SubtipoEventoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un responsable interno.")]
        [Display(Name = "Responsable interno")]
        public int ResponsableInternoId { get; set; }

        [Required(ErrorMessage = "La fecha del evento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha del evento")]
        public DateTime FechaEvento { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Próxima fecha")]
        public DateTime? FechaProxima { get; set; }

        [Display(Name = "Estado del equipo")]
        public string? EstadoEquipoResultado { get; set; }

        [Display(Name = "Comentarios adicionales")]
        public string? ComentariosAdicionales { get; set; }

        [Display(Name = "Es extraordinario")]
        public bool EsExtraordinario { get; set; }

        [Display(Name = "Justificación extraordinario")]
        public string? JustificacionExtraordinario { get; set; }

        [Display(Name = "Número de certificado")]
        public string? NumeroCertificado { get; set; }

        [Required(ErrorMessage = "La fecha de calibración es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de calibración")]
        public DateTime? FechaCalibracion { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un laboratorio.")]
        [Display(Name = "Laboratorio")]
        public int? LaboratorioId { get; set; }

        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "Debe subir el certificado en PDF.")]
        [Display(Name = "Certificado PDF")]
        public IFormFile? ArchivoCertificado { get; set; }

        public List<SelectListItem> Equipos { get; set; } = new();
        public List<SelectListItem> SubtiposEvento { get; set; } = new();
        public List<SelectListItem> ResponsablesInternos { get; set; } = new();
        public List<SelectListItem> Laboratorios { get; set; } = new();
    }
}