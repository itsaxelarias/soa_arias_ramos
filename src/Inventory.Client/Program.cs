using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

internal class Program
{
    /* =============================== Config =============================== */
    const string SERVICE_URL     = "http://localhost:5000/soap/inventario.asmx";
    const string ACTION_INSERT   = "http://tempuri.org/IInventarioSoap/InsertarArticulo";
    const string ACTION_GET      = "http://tempuri.org/IInventarioSoap/ConsultarArticuloPorCodigo";
    const string ACTION_LIST     = "http://tempuri.org/IInventarioSoap/ListarArticulos";
    const string ACTION_UPDATE   = "http://tempuri.org/IInventarioSoap/ActualizarArticulo";
    const string ACTION_DELETE   = "http://tempuri.org/IInventarioSoap/EliminarArticulo";
    const string ACTION_SEARCH   = "http://tempuri.org/IInventarioSoap/BuscarArticulos";
    const string DCNS            = "http://schemas.datacontract.org/2004/07/Inventory.Domain.Soap";
    const string CODE_PREFIX     = "COD-";
    const int    CODE_WIDTH      = 3;

    private record ArticuloVM(
        int Id, string Codigo, string Nombre,
        int CategoriaId, int? ProveedorId,
        decimal PrecioCompra, decimal PrecioVenta,
        int Stock, int StockMin);

    public static async Task Main()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("===== Cliente Inventario (SOAP) =====");
            Console.WriteLine("1) Insertar artículo");
            Console.WriteLine("2) Listar artículos");
            Console.WriteLine("3) Buscar artículos");
            Console.WriteLine("4) Actualizar artículo");
            Console.WriteLine("5) Eliminar artículo");
            Console.WriteLine("6) Salir");
            Console.Write("Selecciona una opción: ");
            var opt = (Console.ReadLine() ?? "").Trim();

            switch (opt)
            {
                case "1":
                    await InsertarArticuloAsync();
                    PressAnyKey();
                    break;
                case "2":
                    await ListarArticulosAsync();
                    PressAnyKey();
                    break;
                case "3":
                    await BuscarArticulosAsync();
                    PressAnyKey();
                    break;
                case "4":
                    await ActualizarArticuloAsync();
                    PressAnyKey();
                    break;
                case "5":
                    await EliminarArticuloAsync();
                    PressAnyKey();
                    break;
                case "6":
                    Console.WriteLine("Saliendo...");
                    return;
                default:
                    Console.WriteLine("Opción inválida.");
                    PressAnyKey();
                    break;
            }
        }
    }

    /* ============================== Nuevas funciones ============================== */

    private static async Task BuscarArticulosAsync()
    {
        Console.WriteLine("\n>> Buscar artículos");
        Console.Write("Buscar por código (Enter=vacío): ");
        var codigo = Console.ReadLine()?.Trim();
        Console.Write("Buscar por nombre (Enter=vacío): ");
        var nombre = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(codigo)) codigo = null;
        if (string.IsNullOrWhiteSpace(nombre)) nombre = null;

        var codigoXml = codigo is null 
            ? "<codigo i:nil=\"true\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" />"
            : $"<codigo>{System.Security.SecurityElement.Escape(codigo)}</codigo>";
        
        var nombreXml = nombre is null
            ? "<nombre i:nil=\"true\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" />"
            : $"<nombre>{System.Security.SecurityElement.Escape(nombre)}</nombre>";

        string envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <BuscarArticulos xmlns=""http://tempuri.org/"">
      {codigoXml}
      {nombreXml}
    </BuscarArticulos>
  </s:Body>
</s:Envelope>";

        var xml = await CallSoap(SERVICE_URL, ACTION_SEARCH, envelope);
        var fault = TryExtractFault(xml);
        if (fault is not null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[SOAP FAULT] " + fault);
            Console.ResetColor();
            return;
        }

        var rows = ParseListado(xml);
        if (rows.Count == 0)
        {
            Console.WriteLine("   (No se encontraron artículos)");
            return;
        }

        MostrarTabla(rows);
    }

    private static async Task ActualizarArticuloAsync()
    {
        Console.WriteLine("\n>> Actualizar artículo");
        Console.Write("Código del artículo a actualizar: ");
        var codigo = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            Console.WriteLine("Código no puede estar vacío.");
            return;
        }

        // Consultar artículo actual
        var articuloActual = await ConsultarArticuloPorCodigoAsync(codigo);
        if (articuloActual is null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"No existe un artículo con código '{codigo}'.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"\nArtículo actual: {articuloActual.Nombre}");
        Console.WriteLine("Ingresa los nuevos valores (Enter=mantener valor actual):\n");

        var nombre = ReadStringOrDefault("Nombre", articuloActual.Nombre);
        var categoriaId = ReadIntOrDefault("Categoría Id", articuloActual.CategoriaId);
        var proveedorId = ReadNullableIntOrDefault("Proveedor Id (0=sin proveedor)", articuloActual.ProveedorId);
        var precioCompra = ReadDecimalOrDefault("Precio compra", articuloActual.PrecioCompra);
        var precioVenta = ReadDecimalOrDefault("Precio venta", articuloActual.PrecioVenta);
        var stock = ReadIntOrDefault("Stock", articuloActual.Stock);
        var stockMin = ReadIntOrDefault("Stock mínimo", articuloActual.StockMin);

        var envelope = BuildUpdateEnvelope(codigo, nombre, categoriaId, proveedorId, precioCompra, precioVenta, stock, stockMin);
        var xml = await CallSoap(SERVICE_URL, ACTION_UPDATE, envelope);

        var fault = TryExtractFault(xml);
        if (fault is not null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[SOAP FAULT] " + fault);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Artículo '{codigo}' actualizado correctamente.");
            Console.ResetColor();
        }
    }

    private static async Task EliminarArticuloAsync()
    {
        Console.WriteLine("\n>> Eliminar artículo");
        Console.Write("Código del artículo a eliminar: ");
        var codigo = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            Console.WriteLine("Código no puede estar vacío.");
            return;
        }

        // Confirmar eliminación
        Console.Write($"¿Estás seguro de eliminar el artículo '{codigo}'? (S/N): ");
        var confirm = Console.ReadLine()?.Trim().ToUpper();
        if (confirm != "S")
        {
            Console.WriteLine("Operación cancelada.");
            return;
        }

        string envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <EliminarArticulo xmlns=""http://tempuri.org/"">
      <codigo>{System.Security.SecurityElement.Escape(codigo)}</codigo>
    </EliminarArticulo>
  </s:Body>
</s:Envelope>";

        var xml = await CallSoap(SERVICE_URL, ACTION_DELETE, envelope);
        var fault = TryExtractFault(xml);
        if (fault is not null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[SOAP FAULT] " + fault);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Artículo '{codigo}' eliminado correctamente.");
            Console.ResetColor();
        }
    }

    /* ============================== Funciones originales ============================== */

    private static async Task InsertarArticuloAsync()
    {
        Console.WriteLine("\n>> Insertar artículo");
        Console.WriteLine("   Sugerencia: existen CategoriaId=1 y ProveedorId=1 en los datos de ejemplo.");

        var nextCode = await NextCodeAsync(CODE_PREFIX, CODE_WIDTH);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   Código asignado automáticamente: {nextCode}");
        Console.ResetColor();

        var nombre       = ReadString("Nombre", "Martillo carpintero");
        var categoriaId  = ReadInt("Categoría Id", min: 1, def: 1);
        var proveedorId  = ReadProveedorNullable();
        var precioCompra = ReadDecimal("Precio compra", min: 0m, def: 5.50m);
        var precioVenta  = ReadDecimal("Precio venta (>= compra)", min: precioCompra, def: 8.99m);
        var stock        = ReadInt("Stock", min: 0, def: 20);
        var stockMin     = ReadInt("Stock mínimo", min: 0, def: 5);

        var envelope = BuildInsertEnvelope(nextCode, nombre, categoriaId, proveedorId, precioCompra, precioVenta, stock, stockMin);
        var xml = await CallSoap(SERVICE_URL, ACTION_INSERT, envelope);

        var fault = TryExtractFault(xml);
        if (fault is not null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[SOAP FAULT] " + fault);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nInsertado correctamente:");
            Console.ResetColor();
        }
    }

    private static async Task ListarArticulosAsync()
    {
        Console.WriteLine("\n>> Listar artículos");
        int limit = ReadInt("Límite de filas", min: 1, def: 200);

        string envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <ListarArticulos xmlns=""http://tempuri.org/"">
      <limit>{limit}</limit>
    </ListarArticulos>
  </s:Body>
</s:Envelope>";

        var xml = await CallSoap(SERVICE_URL, ACTION_LIST, envelope);
        var fault = TryExtractFault(xml);
        if (fault is not null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[SOAP FAULT] " + fault);
            Console.ResetColor();
            return;
        }

        var rows = ParseListado(xml);
        if (rows.Count == 0)
        {
            Console.WriteLine("   (No hay artículos)");
            return;
        }

        MostrarTabla(rows);
    }

    /* ============================== SOAP utils ============================== */

    private static async Task<string> CallSoap(string url, string action, string body)
    {
        using var http = new HttpClient();
        var content = new StringContent(body, Encoding.UTF8, "text/xml");
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml; charset=utf-8");
        content.Headers.Add("SOAPAction", action);
        var resp = await http.PostAsync(url, content);
        return await resp.Content.ReadAsStringAsync();
    }

    private static string BuildInsertEnvelope(string code,
        string nombre, int categoriaId, int? proveedorId,
        decimal precioCompra, decimal precioVenta, int stock, int stockMin)
    {
        var prov = proveedorId.HasValue
            ? $"<d:ProveedorId>{proveedorId.Value}</d:ProveedorId>"
            : "<d:ProveedorId i:nil=\"true\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" />";

        var inv = CultureInfo.InvariantCulture;
        string fmt(decimal v) => v.ToString(inv);

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <InsertarArticulo xmlns=""http://tempuri.org/"">
      <input xmlns:d=""{DCNS}"">
        <d:Codigo>{System.Security.SecurityElement.Escape(code)}</d:Codigo>
        <d:Nombre>{System.Security.SecurityElement.Escape(nombre)}</d:Nombre>
        <d:CategoriaId>{categoriaId}</d:CategoriaId>
        {prov}
        <d:PrecioCompra>{fmt(precioCompra)}</d:PrecioCompra>
        <d:PrecioVenta>{fmt(precioVenta)}</d:PrecioVenta>
        <d:Stock>{stock}</d:Stock>
        <d:StockMin>{stockMin}</d:StockMin>
      </input>
    </InsertarArticulo>
  </s:Body>
</s:Envelope>";
    }

    private static string BuildUpdateEnvelope(string codigo, string nombre, int categoriaId, int? proveedorId,
        decimal precioCompra, decimal precioVenta, int stock, int stockMin)
    {
        var prov = proveedorId.HasValue
            ? $"<d:ProveedorId>{proveedorId.Value}</d:ProveedorId>"
            : "<d:ProveedorId i:nil=\"true\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" />";

        var inv = CultureInfo.InvariantCulture;
        string fmt(decimal v) => v.ToString(inv);

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <ActualizarArticulo xmlns=""http://tempuri.org/"">
      <codigo>{System.Security.SecurityElement.Escape(codigo)}</codigo>
      <input xmlns:d=""{DCNS}"">
        <d:Codigo>{System.Security.SecurityElement.Escape(codigo)}</d:Codigo>
        <d:Nombre>{System.Security.SecurityElement.Escape(nombre)}</d:Nombre>
        <d:CategoriaId>{categoriaId}</d:CategoriaId>
        {prov}
        <d:PrecioCompra>{fmt(precioCompra)}</d:PrecioCompra>
        <d:PrecioVenta>{fmt(precioVenta)}</d:PrecioVenta>
        <d:Stock>{stock}</d:Stock>
        <d:StockMin>{stockMin}</d:StockMin>
      </input>
    </ActualizarArticulo>
  </s:Body>
</s:Envelope>";
    }

    private static async Task<ArticuloVM?> ConsultarArticuloPorCodigoAsync(string codigo)
{
    string env = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <ConsultarArticuloPorCodigo xmlns=""http://tempuri.org/"">
      <codigo>{System.Security.SecurityElement.Escape(codigo)}</codigo>
    </ConsultarArticuloPorCodigo>
  </s:Body>
</s:Envelope>";
    
    var resp = await CallSoap(SERVICE_URL, ACTION_GET, env);
    
    // Usa el nuevo método de parsing
    return ParseArticuloUnico(resp);
}

// NUEVO MÉTODO: Parsea la respuesta de ConsultarArticuloPorCodigo
private static ArticuloVM? ParseArticuloUnico(string xml)
{
    try
    {
        var x = XDocument.Parse(xml);
        
        // Busca el resultado - puede estar en varios lugares
        var result = x.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ConsultarArticuloPorCodigoResult");
        
        if (result == null) return null;
        
        // Verifica si es nil (null)
        var nilAttr = result.Attributes()
            .FirstOrDefault(a => a.Name.LocalName == "nil");
        if (nilAttr != null && nilAttr.Value == "true") return null;
        
        // Parsea los campos
        int TryInt(string n, int def = 0)
        {
            var el = result.Descendants().FirstOrDefault(e => e.Name.LocalName == n);
            return int.TryParse(el?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
        }
        
        int? TryNullableInt(string n)
        {
            var el = result.Descendants().FirstOrDefault(e => e.Name.LocalName == n);
            if (el == null) return null;
            if (el.Attributes().Any(a => a.Name.LocalName == "nil" && a.Value == "true")) return null;
            return int.TryParse(el.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : (int?)null;
        }
        
        decimal TryDec(string n, decimal def = 0m)
        {
            var el = result.Descendants().FirstOrDefault(e => e.Name.LocalName == n);
            return decimal.TryParse(el?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
        }
        
        string TryStr(string n, string def = "")
        {
            var el = result.Descendants().FirstOrDefault(e => e.Name.LocalName == n);
            return el?.Value ?? def;
        }
        
        return new ArticuloVM(
            Id: TryInt("Id"),
            Codigo: TryStr("Codigo"),
            Nombre: TryStr("Nombre"),
            CategoriaId: TryInt("CategoriaId"),
            ProveedorId: TryNullableInt("ProveedorId"),
            PrecioCompra: TryDec("PrecioCompra"),
            PrecioVenta: TryDec("PrecioVenta"),
            Stock: TryInt("Stock"),
            StockMin: TryInt("StockMin")
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parseando respuesta: {ex.Message}");
        return null;
    }
}

    /* ============================ Parse helpers ============================ */

    private static List<ArticuloVM> ParseListado(string xml)
    {
        var rows = new List<ArticuloVM>();
        try
        {
            var x = XDocument.Parse(xml);
            var items = x.Descendants().Where(e => e.Name.LocalName is "ArticuloSoap");
            foreach (var it in items)
            {
                int TryInt(string n, int def = 0) =>
                    int.TryParse(it.Descendants().FirstOrDefault(e => e.Name.LocalName == n)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;

                int? TryNullableInt(string n)
                {
                    var el = it.Descendants().FirstOrDefault(e => e.Name.LocalName == n);
                    if (el == null) return null;
                    if (el.Attributes().Any(a => a.Name.LocalName == "nil" && a.Value == "true")) return null;
                    return int.TryParse(el.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : (int?)null;
                }

                decimal TryDec(string n, decimal def = 0m) =>
                    decimal.TryParse(it.Descendants().FirstOrDefault(e => e.Name.LocalName == n)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;

                string TryStr(string n, string def = "") =>
                    it.Descendants().FirstOrDefault(e => e.Name.LocalName == n)?.Value ?? def;

                rows.Add(new ArticuloVM(
                    Id: TryInt("Id"),
                    Codigo: TryStr("Codigo"),
                    Nombre: TryStr("Nombre"),
                    CategoriaId: TryInt("CategoriaId"),
                    ProveedorId: TryNullableInt("ProveedorId"),
                    PrecioCompra: TryDec("PrecioCompra"),
                    PrecioVenta: TryDec("PrecioVenta"),
                    Stock: TryInt("Stock"),
                    StockMin: TryInt("StockMin")
                ));
            }
        }
        catch { }
        return rows;
    }

    private static string? TryExtractFault(string xml)
    {
        try
        {
            var x = XDocument.Parse(xml);
            return x.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value?.Trim();
        }
        catch { return null; }
    }

    /* ============================ Code helpers ============================ */

    private static async Task<bool> CodeExistsAsync(string code)
    {
        string env = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <ConsultarArticuloPorCodigo xmlns=""http://tempuri.org/"">
      <codigo>{System.Security.SecurityElement.Escape(code)}</codigo>
    </ConsultarArticuloPorCodigo>
  </s:Body>
</s:Envelope>";
        var resp = await CallSoap(SERVICE_URL, ACTION_GET, env);
        return XDocument.Parse(resp).Descendants().Any(e => e.Name.LocalName == "ConsultarArticuloPorCodigoResult");
    }

    private static async Task<string> NextCodeAsync(string prefix, int width, int max = 9999)
    {
        for (int n = 1; n <= max; n++)
        {
            var code = $"{prefix}{n.ToString("D" + width)}";
            if (!await CodeExistsAsync(code)) return code;
        }
        throw new InvalidOperationException("No hay código disponible en el rango.");
    }

    /* ================================ UI helpers ================================ */

    private static string ReadString(string prompt, string? def = null, int maxLen = 120)
    {
        while (true)
        {
            Console.Write($"{prompt}{(def is null ? "" : $" [{def}]")}: ");
            var s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s))
            {
                if (def is not null) return def;
                Console.WriteLine("  * No puede estar vacío."); continue;
            }
            s = s.Trim();
            if (s.Length > maxLen) { Console.WriteLine($"  * Máximo {maxLen} caracteres."); continue; }
            return s;
        }
    }

    private static string ReadStringOrDefault(string prompt, string defaultValue)
    {
        Console.Write($"{prompt} [{defaultValue}]: ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }

    private static int ReadIntOrDefault(string prompt, int defaultValue)
    {
        Console.Write($"{prompt} [{defaultValue}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input)) return defaultValue;
        return int.TryParse(input, out var v) ? v : defaultValue;
    }

    private static int? ReadNullableIntOrDefault(string prompt, int? defaultValue)
    {
        var defStr = defaultValue.HasValue ? defaultValue.Value.ToString() : "sin proveedor";
        Console.Write($"{prompt} [{defStr}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input)) return defaultValue;
        if (input == "0") return null;
        return int.TryParse(input, out var v) ? v : defaultValue;
    }

    private static decimal ReadDecimalOrDefault(string prompt, decimal defaultValue)
    {
        var inv = CultureInfo.InvariantCulture;
        Console.Write($"{prompt} [{defaultValue.ToString(inv)}]: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input)) return defaultValue;
        input = NormalizeDecimal(input);
        return decimal.TryParse(input, NumberStyles.Number, inv, out var v) ? v : defaultValue;
    }

    private static string NormalizeDecimal(string s)
    {
        s = (s ?? "").Trim();
        if (s.Contains(',')) s = s.Replace(".", "").Replace(',', '.');
        return s;
    }

    private static decimal ReadDecimal(string prompt, decimal? min = null, decimal? def = null)
    {
        var inv = CultureInfo.InvariantCulture;
        while (true)
        {
            Console.Write($"{prompt}{(def.HasValue ? $" [{def.Value.ToString(inv)}]" : "")} (coma o punto): ");
            var s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s) && def.HasValue) return def.Value;
            s = NormalizeDecimal(s ?? "");
            if (decimal.TryParse(s, NumberStyles.Number, inv, out var v))
            {
                if (min.HasValue && v < min.Value) { Console.WriteLine($"  * Debe ser >= {min.Value.ToString(inv)}."); continue; }
                return v;
            }
            Console.WriteLine("  * Decimal inválido.");
        }
    }

    private static int ReadInt(string prompt, int? min = null, int? def = null)
    {
        while (true)
        {
            Console.Write($"{prompt}{(def.HasValue ? $" [{def}]" : "")}: ");
            var s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s) && def.HasValue) return def.Value;
            if (int.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            {
                if (min.HasValue && v < min.Value) { Console.WriteLine($"  * Debe ser >= {min}."); continue; }
                return v;
            }
            Console.WriteLine("  * Entero inválido.");
        }
    }

    private static int? ReadProveedorNullable()
    {
        while (true)
        {
            Console.Write("Proveedor Id (Enter vacío, 0 = sin proveedor): ");
            var s = (Console.ReadLine() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            {
                if (v == 0) return null;
                if (v < 0) { Console.WriteLine("  * Debe ser >= 0."); continue; }
                return v;
            }
            Console.WriteLine("  * Entero inválido o vacío.");
        }
    }

    private static void PressAnyKey()
    {
        Console.WriteLine("\nPresiona ENTER para volver al menú...");
        Console.ReadLine();
    }

    /* ============================ Tabla ============================ */

    private static void MostrarTabla(List<ArticuloVM> rows)
    {
        var inv = CultureInfo.InvariantCulture;
        var headers = new[] { "ID", "Código", "Nombre", "P.Compra", "P.Venta", "Stock", "Min" };

        var data = rows
            .OrderBy(r => r.Id)
            .Select(r => new[]
            {
                r.Id.ToString(inv),
                r.Codigo,
                (r.Nombre?.Length ?? 0) > 30 ? r.Nombre.Substring(0, 30) + "…" : r.Nombre,
                r.PrecioCompra.ToString("0.##", inv),
                r.PrecioVenta.ToString("0.##", inv),
                r.Stock.ToString(inv),
                r.StockMin.ToString(inv)
            })
            .ToList();

        PrintAsciiTable(headers, data);
    }

    private static void PrintAsciiTable(string[] headers, List<string[]> rows)
    {
        int cols = headers.Length;
        int[] widths = new int[cols];

        for (int i = 0; i < cols; i++)
            widths[i] = headers[i].Length;

        foreach (var r in rows)
            for (int i = 0; i < cols && i < r.Length; i++)
                widths[i] = Math.Max(widths[i], r[i]?.Length ?? 0);

        string Border() => "+" + string.Join("+", widths.Select(w => new string('-', w + 2))) + "+";
        string RowLine(string[] cells) =>
            "| " + string.Join(" | ", cells.Select((c, i) =>
            {
                c ??= "";
                bool numeric = i is 0 or 3 or 4 or 5 or 6;
                return numeric ? c.PadLeft(widths[i]) : c.PadRight(widths[i]);
            })) + " |";

        var border = Border();
        Console.WriteLine();
        Console.WriteLine(border);
        Console.WriteLine(RowLine(headers));
        Console.WriteLine(border);
        foreach (var r in rows)
            Console.WriteLine(RowLine(r));
        Console.WriteLine(border);
    }
}