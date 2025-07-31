using CarnetAduaneroProcessor.Core.Models;

namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Interfaz para el repositorio de Carnés Aduaneros
    /// </summary>
    public interface ICarnetAduaneroRepository
    {
        /// <summary>
        /// Obtiene todos los carnés con paginación
        /// </summary>
        Task<IEnumerable<CarnetAduanero>> ObtenerTodosAsync(int page = 1, int pageSize = 20, string? search = null);

        /// <summary>
        /// Obtiene un carné por su ID
        /// </summary>
        Task<CarnetAduanero?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Obtiene un carné por su número
        /// </summary>
        Task<CarnetAduanero?> ObtenerPorNumeroAsync(string numeroCarnet);

        /// <summary>
        /// Agrega un nuevo carné
        /// </summary>
        Task<CarnetAduanero> AgregarAsync(CarnetAduanero carnet);

        /// <summary>
        /// Actualiza un carné existente
        /// </summary>
        Task<CarnetAduanero> ActualizarAsync(CarnetAduanero carnet);

        /// <summary>
        /// Elimina un carné
        /// </summary>
        Task<bool> EliminarAsync(int id);

        /// <summary>
        /// Obtiene estadísticas del sistema
        /// </summary>
        Task<object> ObtenerEstadisticasAsync();

        /// <summary>
        /// Verifica si existe un carné con el número especificado
        /// </summary>
        Task<bool> ExistePorNumeroAsync(string numeroCarnet);

        /// <summary>
        /// Obtiene el total de carnés
        /// </summary>
        Task<int> ObtenerTotalAsync();
    }
} 