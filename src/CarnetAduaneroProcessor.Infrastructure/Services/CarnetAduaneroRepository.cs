using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using System.Collections.Generic;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Repositorio en memoria para Carnés Aduaneros
    /// </summary>
    public class CarnetAduaneroRepository : ICarnetAduaneroRepository
    {
        private readonly List<CarnetAduanero> _carnets;
        private int _nextId = 1;
        private readonly object _lock = new object();

        public CarnetAduaneroRepository()
        {
            _carnets = new List<CarnetAduanero>();
            
            // Agregar algunos datos de ejemplo
            AgregarDatosEjemplo();
        }

        /// <summary>
        /// Obtiene todos los carnés con paginación
        /// </summary>
        public async Task<IEnumerable<CarnetAduanero>> ObtenerTodosAsync(int page = 1, int pageSize = 20, string? search = null)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var query = _carnets.AsQueryable();

                    // Aplicar filtro de búsqueda
                    if (!string.IsNullOrEmpty(search))
                    {
                        query = query.Where(c =>
                            c.NumeroCarnet.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            c.NombreTitular.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            c.Rut.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (c.ApellidosTitular != null && c.ApellidosTitular.Contains(search, StringComparison.OrdinalIgnoreCase))
                        );
                    }

                    // Aplicar paginación
                    return query
                        .OrderByDescending(c => c.FechaCreacion)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }
            });
        }

        /// <summary>
        /// Obtiene un carné por su ID
        /// </summary>
        public async Task<CarnetAduanero?> ObtenerPorIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _carnets.FirstOrDefault(c => c.Id == id);
                }
            });
        }

        /// <summary>
        /// Obtiene un carné por su número
        /// </summary>
        public async Task<CarnetAduanero?> ObtenerPorNumeroAsync(string numeroCarnet)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _carnets.FirstOrDefault(c => c.NumeroCarnet == numeroCarnet);
                }
            });
        }

        /// <summary>
        /// Agrega un nuevo carné
        /// </summary>
        public async Task<CarnetAduanero> AgregarAsync(CarnetAduanero carnet)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    carnet.Id = _nextId++;
                    carnet.FechaCreacion = DateTime.UtcNow;
                    carnet.FechaModificacion = DateTime.UtcNow;
                    
                    _carnets.Add(carnet);
                    
                    return carnet;
                }
            });
        }

        /// <summary>
        /// Actualiza un carné existente
        /// </summary>
        public async Task<CarnetAduanero> ActualizarAsync(CarnetAduanero carnet)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var existingCarnet = _carnets.FirstOrDefault(c => c.Id == carnet.Id);
                    if (existingCarnet == null)
                    {
                        throw new ArgumentException($"No se encontró el carné con ID {carnet.Id}");
                    }

                    // Actualizar propiedades
                    existingCarnet.NumeroCarnet = carnet.NumeroCarnet;
                    existingCarnet.NombreTitular = carnet.NombreTitular;
                    existingCarnet.ApellidosTitular = carnet.ApellidosTitular;
                    existingCarnet.Rut = carnet.Rut;
                    existingCarnet.FechaEmision = carnet.FechaEmision;
                    existingCarnet.FechaVencimiento = carnet.FechaVencimiento;
                    existingCarnet.Resolucion = carnet.Resolucion;
                    existingCarnet.AgadCod = carnet.AgadCod;
                    existingCarnet.EntidadEmisora = carnet.EntidadEmisora;
                    existingCarnet.Estado = carnet.Estado;
                    existingCarnet.Comentarios = carnet.Comentarios;
                    existingCarnet.FechaModificacion = DateTime.UtcNow;

                    return existingCarnet;
                }
            });
        }

        /// <summary>
        /// Elimina un carné
        /// </summary>
        public async Task<bool> EliminarAsync(int id)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var carnet = _carnets.FirstOrDefault(c => c.Id == id);
                    if (carnet == null)
                    {
                        return false;
                    }

                    _carnets.Remove(carnet);
                    
                    return true;
                }
            });
        }

        /// <summary>
        /// Obtiene estadísticas del sistema
        /// </summary>
        public async Task<object> ObtenerEstadisticasAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var totalCarnets = _carnets.Count;
                    var carnetsVigentes = _carnets.Count(c => c.Estado == "Vigente");
                    var carnetsVencidos = _carnets.Count(c => c.Estado == "Vencido");
                    var carnetsSinEstado = totalCarnets - carnetsVigentes - carnetsVencidos;

                    return new
                    {
                        totalCarnets,
                        carnetsVigentes,
                        carnetsVencidos,
                        carnetsSinEstado,
                        fechaActual = DateTime.UtcNow
                    };
                }
            });
        }

        /// <summary>
        /// Verifica si existe un carné con el número especificado
        /// </summary>
        public async Task<bool> ExistePorNumeroAsync(string numeroCarnet)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _carnets.Any(c => c.NumeroCarnet == numeroCarnet);
                }
            });
        }

        /// <summary>
        /// Obtiene el total de carnés
        /// </summary>
        public async Task<int> ObtenerTotalAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _carnets.Count;
                }
            });
        }

        /// <summary>
        /// Agrega datos de ejemplo para demostración
        /// </summary>
        private void AgregarDatosEjemplo()
        {
            var carnetsEjemplo = new List<CarnetAduanero>
            {
                new CarnetAduanero
                {
                    NumeroCarnet = "N8687",
                    NombreTitular = "GONZALO ADOLFO",
                    ApellidosTitular = "GONZALEZ PINO",
                    Rut = "15.970.128-K",
                    FechaEmision = new DateTime(2024, 1, 17),
                    FechaVencimiento = new DateTime(2025, 1, 17),
                    Resolucion = "0142",
                    AgadCod = "E-12",
                    EntidadEmisora = "anagena - asociación nacional de agentes de aduanas",
                    Estado = "Vigente",
                    NombreArchivo = "carnet_ejemplo_1.pdf",
                    ConfianzaExtraccion = 0.95m,
                    MetodoExtraccion = "Extracción Manual",
                    Comentarios = "Carné de ejemplo para demostración"
                },
                new CarnetAduanero
                {
                    NumeroCarnet = "N1234",
                    NombreTitular = "MARÍA JOSÉ",
                    ApellidosTitular = "RODRÍGUEZ LÓPEZ",
                    Rut = "12.345.678-9",
                    FechaEmision = new DateTime(2023, 6, 15),
                    FechaVencimiento = new DateTime(2024, 6, 15),
                    Resolucion = "0234",
                    AgadCod = "E-15",
                    EntidadEmisora = "anagena - asociación nacional de agentes de aduanas",
                    Estado = "Vencido",
                    NombreArchivo = "carnet_ejemplo_2.pdf",
                    ConfianzaExtraccion = 0.88m,
                    MetodoExtraccion = "Extracción Manual",
                    Comentarios = "Carné vencido de ejemplo"
                },
                new CarnetAduanero
                {
                    NumeroCarnet = "N5678",
                    NombreTitular = "CARLOS ALBERTO",
                    ApellidosTitular = "MARTÍNEZ SOTO",
                    Rut = "18.765.432-1",
                    FechaEmision = new DateTime(2024, 3, 10),
                    FechaVencimiento = new DateTime(2025, 3, 10),
                    Resolucion = "0456",
                    AgadCod = "E-08",
                    EntidadEmisora = "anagena - asociación nacional de agentes de aduanas",
                    Estado = "Vigente",
                    NombreArchivo = "carnet_ejemplo_3.pdf",
                    ConfianzaExtraccion = 0.92m,
                    MetodoExtraccion = "Extracción Manual",
                    Comentarios = "Carné reciente de ejemplo"
                }
            };

            foreach (var carnet in carnetsEjemplo)
            {
                carnet.Id = _nextId++;
                carnet.FechaCreacion = DateTime.UtcNow;
                carnet.FechaModificacion = DateTime.UtcNow;
                _carnets.Add(carnet);
            }
        }
    }
} 