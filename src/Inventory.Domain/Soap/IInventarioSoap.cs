using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Inventory.Domain.Soap
{
    // Contrato principal del servicio SOAP
    [ServiceContract(Name = "IInventarioSoap", Namespace = "http://tempuri.org/")]
    public interface IInventarioSoap
    {
        [OperationContract(
            Action = "http://tempuri.org/IInventarioSoap/InsertarArticulo",
            ReplyAction = "*")]
        ArticuloSoap InsertarArticulo(ArticuloInputSoap input);

        [OperationContract(
            Action = "http://tempuri.org/IInventarioSoap/ConsultarArticuloPorCodigo",
            ReplyAction = "*")]
        ArticuloSoap? ConsultarArticuloPorCodigo(string codigo);

        [OperationContract(
            Action = "http://tempuri.org/IInventarioSoap/ListarArticulos",
            ReplyAction = "*")]
        IEnumerable<ArticuloSoap> ListarArticulos(int limit);
    }

    // ======== Clases de datos ========

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Inventory.Domain.Soap")]
    public class ArticuloInputSoap
    {
        [DataMember(Order = 1)] public string Codigo { get; set; } = string.Empty;
        [DataMember(Order = 2)] public string Nombre { get; set; } = string.Empty;
        [DataMember(Order = 3)] public int CategoriaId { get; set; }
        [DataMember(Order = 4)] public int? ProveedorId { get; set; }
        [DataMember(Order = 5)] public decimal PrecioCompra { get; set; }
        [DataMember(Order = 6)] public decimal PrecioVenta { get; set; }
        [DataMember(Order = 7)] public int Stock { get; set; }
        [DataMember(Order = 8)] public int StockMin { get; set; }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Inventory.Domain.Soap")]
    public class ArticuloSoap
    {
        [DataMember(Order = 1)] public int Id { get; set; }
        [DataMember(Order = 2)] public string Codigo { get; set; } = string.Empty;
        [DataMember(Order = 3)] public string Nombre { get; set; } = string.Empty;
        [DataMember(Order = 4)] public int CategoriaId { get; set; }
        [DataMember(Order = 5)] public int? ProveedorId { get; set; }
        [DataMember(Order = 6)] public decimal PrecioCompra { get; set; }
        [DataMember(Order = 7)] public decimal PrecioVenta { get; set; }
        [DataMember(Order = 8)] public int Stock { get; set; }
        [DataMember(Order = 9)] public int StockMin { get; set; }
    }
}
