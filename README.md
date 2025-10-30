Guía de instalación y despliegue (Manual Técnico)
Descripción del proyecto

Este sistema implementa una arquitectura en N-capas con servicios SOAP (SoapCore), base de datos PostgreSQL, y dos clientes de consumo:

Cliente de consola (.NET) — Permite insertar y listar artículos mediante peticiones SOAP.

Cliente web (HTML + JS) — Interfaz moderna y responsiva que consume el mismo servicio SOAP desde el navegador.

El objetivo es demostrar interoperabilidad entre diferentes tecnologías consumiendo un mismo servicio web.

Estructura del proyecto
inventory-ncapas/
│
├── src/
│   ├── Inventory.Domain/        # Entidades, contratos e interfaces comunes
│   ├── Inventory.Data/          # Capa de acceso a datos (Dapper, repositorios)
│   ├── Inventory.Business/      # Lógica de negocio, validaciones y servicios
│   ├── Inventory.SOAP/          # Host del servicio SOAP (SoapCore)
│   └── Inventory.Client/        # Cliente de consola .NET
│
├── web-client/                  # Cliente web (HTML, JS, CSS)
│   ├── index.html
│   └── assets/
│
└── README.md

Requisitos previos
Componente	Versión recomendada	Descripción
.NET SDK	8.0 o superior	Para compilar y ejecutar el backend SOAP
PostgreSQL	15 o superior	Base de datos para los artículos
Python	3.10 o superior	Para servir el cliente web localmente
Dapper	Última estable	ORM liviano utilizado por la capa Data
SoapCore	Última estable	Framework para exponer servicios SOAP
Configuración de la base de datos

Crear la base de datos:

CREATE DATABASE inventorydb;


Crear las tablas necesarias:

CREATE TABLE categoria (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100)
);

CREATE TABLE proveedor (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100),
    ruc VARCHAR(20)
);

CREATE TABLE articulo (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(50) UNIQUE NOT NULL,
    nombre VARCHAR(150),
    categoria_id INT REFERENCES categoria(id),
    proveedor_id INT REFERENCES proveedor(id),
    precio_compra DECIMAL(10,2),
    precio_venta DECIMAL(10,2),
    stock INT,
    stock_min INT,
    creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


Insertar datos de ejemplo:

INSERT INTO categoria (nombre) VALUES ('Ferretería');
INSERT INTO proveedor (nombre, ruc) VALUES ('FerreProve S.A.', '1799999999001');


Configurar la conexión en:

src/Inventory.SOAP/appsettings.json


Ejemplo:

{
  "ConnectionStrings": {
    "Pg": "Host=localhost;Port=5432;Database=inventorydb;Username=invuser;Password=12345"
  }
}

Despliegue del servidor SOAP

Abrir la carpeta raíz del proyecto.

Compilar y ejecutar el servicio SOAP:

dotnet run --project .\src\Inventory.SOAP --urls http://localhost:5000


Verificar el servicio:

Abrir en navegador: http://localhost:5000/soap/inventario.asmx?wsdl

Si se muestra el WSDL, el servicio está funcionando correctamente.

Ejecución del cliente de consola (.NET)

Abrir una nueva terminal y ejecutar:

dotnet run --project .\src\Inventory.Client


Menú disponible:

===== Cliente Inventario (SOAP) =====
1) Insertar artículo
2) Listar artículos
3) Salir


El cliente se conecta al servicio en http://localhost:5000/soap/inventario.asmx.

Ejecución del cliente web
Paso 1 — Servir el sitio web localmente

Desde la carpeta /web-client ejecutar:

python -m http.server 8080


Luego abrir en navegador:
http://localhost:8080

Paso 2 — Configurar CORS en el servidor SOAP

Asegurarse de tener las siguientes líneas en Program.cs del proyecto Inventory.SOAP:

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

app.UseRouting();
app.UseCors("WebClient");

Paso 3 — Uso del cliente web

Botón “Listar”: realiza una llamada SOAP y muestra los artículos en una tabla.

Botón “Nuevo”: abre un formulario para insertar artículos.

Botón “Refrescar”: actualiza los datos en pantalla.

Pruebas de interoperabilidad
Cliente	Tecnología	Estado
Cliente .NET	C# / Consola	Correcto
Cliente Web	HTML + JavaScript (fetch SOAP)	Correcto
Servicio SOAP	ASP.NET + SoapCore	Correcto
Base de datos	PostgreSQL	Correcto
Errores comunes
Error	Causa	Solución
Failed to fetch	Problema de CORS	Agregar app.UseCors("WebClient"); antes de UseEndpoints()
Role postgres does not exist	Usuario mal configurado	Crear usuario invuser o ajustar appsettings.json
SOAP FAULT	Datos inválidos o claves foráneas	Verificar categoria_id y proveedor_id existentes
Port in use	Puerto 5000 ocupado	Cambiar con --urls http://localhost:5199
Despliegue final

Para publicar el servicio:

dotnet publish .\src\Inventory.SOAP -c Release -o ./publish


Ejecutar el servicio publicado:

dotnet ./publish/Inventory.SOAP.dll --urls http://0.0.0.0:5000


El cliente web puede desplegarse en un hosting estático (GitHub Pages, Netlify, Vercel, etc.).

Créditos

Proyecto desarrollado por:
Anthony Arias y Jesus Ramos
Universidad de las Fuerzas Armadas ESPE
Materia: Aplicaciones Distribuidas
Año: 2025