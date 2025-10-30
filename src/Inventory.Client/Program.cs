using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using System.Xml;

internal class Program
{
    /* =============================== Config =============================== */
    const string SERVICE_URL   = "http://localhost:5000/soap/inventario.asmx";
    const string ACTION_INSERT = "http://tempuri.org/IInventarioSoap/InsertarArticulo";
    const string ACTION_GET    = "http://tempuri.org/IInventarioSoap/ConsultarArticuloPorCodigo";
    const string ACTION_LIST   = "http://tempuri.org/IInventarioSoap/ListarArticulos";
    const string DCNS          = "http://schemas.datacontract.org/2004/07/Inventory.Domain.Soap";
    const string CODE_PREFIX   = "COD-";
    const int    CODE_WIDTH    = 3; // COD-001

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
            Console.WriteLine("3) Salir");
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
                    Console.WriteLine("Saliendo...");
                    return;
                default:
                    Console.WriteLine("Opción inválida.");
                    PressAnyKey();
                    break;
            }
        }
    }

    /* ============================== Menú acciones ============================== */

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
            Console.WriteLine(Pretty(xml).ToString());
        }
    }

    private static async Task ListarArticulosAsync()
    {
        Console.WriteLine("\n>> Listar artículos (una sola llamada)");
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

    /* ============================ Pretty & Tabla ============================ */

    private static XDocument Pretty(string xml)
    {
        try { return XDocument.Parse(xml); }
        catch { return XDocument.Parse($"<raw>{System.Security.SecurityElement.Escape(xml)}</raw>"); }
    }

    // Dibuja una tabla ASCII elegante
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
