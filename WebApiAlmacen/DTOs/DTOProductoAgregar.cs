using WebApiAlmacen.Validators;

namespace WebApiAlmacen.DTOs
{
    public class DTOProductoAgregar
    {
        public string Nombre { get; set; }
        [PrecioValidacion]
        public decimal Precio { get; set; }
        // Validadores
        // Los validadores nos van a permitir validar la información que nos llega
        [PesoArchivoValidacion(PesoMaximoEnMegaBytes: 5)]
        [TipoArchivoValidacion(grupoTipoArchivo: GrupoTipoArchivo.Documentos)]
        public IFormFile Foto { get; set; }
        public int FamiliaId { get; set; }

    }
}
