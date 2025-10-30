CREATE TABLE IF NOT EXISTS categoria (
  id SERIAL PRIMARY KEY,
  nombre VARCHAR(80) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS proveedor (
  id SERIAL PRIMARY KEY,
  nombre VARCHAR(120) NOT NULL,
  ruc VARCHAR(20) UNIQUE
);

CREATE TABLE IF NOT EXISTS articulo (
  id SERIAL PRIMARY KEY,
  codigo VARCHAR(50) NOT NULL UNIQUE,
  nombre VARCHAR(120) NOT NULL,
  categoria_id INT NOT NULL REFERENCES categoria(id),
  proveedor_id INT REFERENCES proveedor(id),
  precio_compra NUMERIC(12,2) NOT NULL CHECK (precio_compra >= 0),
  precio_venta NUMERIC(12,2) NOT NULL CHECK (precio_venta >= 0),
  stock INT NOT NULL CHECK (stock >= 0),
  stock_min INT NOT NULL DEFAULT 0 CHECK (stock_min >= 0),
  creado_en TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Datos de prueba mínimos
INSERT INTO categoria (nombre) VALUES ('Herrajes') ON CONFLICT DO NOTHING;
INSERT INTO proveedor (nombre, ruc) VALUES ('FerreProve S.A.', '1799999999001')
  ON CONFLICT (ruc) DO NOTHING;
