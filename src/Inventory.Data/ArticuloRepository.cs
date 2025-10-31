using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Inventory.Domain.Contracts;
using Inventory.Domain.Entities;
using Npgsql;

namespace Inventory.Data
{
    public class ArticuloRepository : IArticuloRepository
    {
        private readonly DbConfig _cfg;

        public ArticuloRepository(DbConfig cfg)
        {
            _cfg = cfg;
        }

        private IDbConnection CreateConn() => new NpgsqlConnection(_cfg.ConnectionString);

        public async Task<int> InsertAsync(Articulo entidad)
        {
            const string sql = @"
INSERT INTO articulo
    (codigo, nombre, categoria_id, proveedor_id, precio_compra, precio_venta, stock, stock_min)
VALUES
    (@Codigo, @Nombre, @CategoriaId, @ProveedorId, @PrecioCompra, @PrecioVenta, @Stock, @StockMin)
RETURNING id;";

            using var conn = CreateConn();
            var id = await conn.ExecuteScalarAsync<int>(sql, entidad);
            entidad.Id = id;
            return id;
        }

        public async Task<int> UpdateAsync(Articulo entidad)
        {
            const string sql = @"
UPDATE articulo
   SET nombre        = @Nombre,
       categoria_id  = @CategoriaId,
       proveedor_id  = @ProveedorId,
       precio_compra = @PrecioCompra,
       precio_venta  = @PrecioVenta,
       stock         = @Stock,
       stock_min     = @StockMin
 WHERE codigo = @Codigo;   -- o WHERE id = @Id si prefieres
";
            using var conn = CreateConn();
            return await conn.ExecuteAsync(sql, entidad);
        }

        public async Task<Articulo?> GetByCodigoAsync(string codigo)
        {
            const string sql = @"
        SELECT id, codigo, nombre, categoria_id, proveedor_id,
            precio_compra, precio_venta, stock, stock_min
        FROM articulo
        WHERE TRIM(UPPER(codigo)) = TRIM(UPPER(@Codigo))
        LIMIT 1;";
            
            using var conn = CreateConn();
            return await conn.QueryFirstOrDefaultAsync<Articulo>(sql, new { Codigo = codigo });
        }
        public async Task<IEnumerable<Articulo>> SearchAsync(string? codigo, string? nombre)
        {
            const string sql = @"
SELECT id, codigo, nombre, categoria_id, proveedor_id,
       precio_compra, precio_venta, stock, stock_min
  FROM articulo
 WHERE (@Codigo IS NULL OR codigo ILIKE '%'||@Codigo||'%')
   AND (@Nombre IS NULL OR nombre ILIKE '%'||@Nombre||'%')
 ORDER BY id;";
            using var conn = CreateConn();
            return await conn.QueryAsync<Articulo>(sql, new { Codigo = codigo, Nombre = nombre });
        }

        // Alternativa si prefieres:
        // public async Task<IEnumerable<Articulo>> GetAllAsync()
        // {
        //     const string sql = @"SELECT id, codigo, nombre, categoria_id, proveedor_id,
        //                                 precio_compra, precio_venta, stock, stock_min
        //                          FROM articulo ORDER BY id;";
        //     using var conn = CreateConn();
        //     return await conn.QueryAsync<Articulo>(sql);
        // }

        // En ArticuloRepository.cs
        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM articulo WHERE id = @Id;";
            using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { Id = id });
        }
    }
}
