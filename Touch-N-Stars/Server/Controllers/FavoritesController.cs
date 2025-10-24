using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TouchNStars.Server.Controllers;

/// <summary>
/// API Controller for favorite targets management
/// </summary>
public class FavoritesController : WebApiController
{
    private static readonly string FavoritesFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NINA", "TnsCache", "favorites.json"
    );
    private static readonly object _fileLock = new();

    /// <summary>
    /// POST /api/favorites - Add a new favorite target
    /// </summary>
    [Route(HttpVerbs.Post, "/favorites")]
    public async Task<ApiResponse> AddFavoriteTarget()
    {
        try
        {
            var favorite = await HttpContext.GetRequestDataAsync<FavoriteTarget>();

            if (favorite.Id == Guid.Empty)
                favorite.Id = Guid.NewGuid();

            List<FavoriteTarget> currentFavorites = new();
            if (File.Exists(FavoritesFilePath))
            {
                var json = await File.ReadAllTextAsync(FavoritesFilePath);
                currentFavorites = System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);
            }

            currentFavorites.Add(favorite);
            lock (_fileLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FavoritesFilePath));
                var updatedJson = System.Text.Json.JsonSerializer.Serialize(
                    currentFavorites,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(FavoritesFilePath, updatedJson);
            }

            return new ApiResponse
            {
                Success = true,
                Response = favorite,
                StatusCode = 200,
                Type = "FavoriteTarget"
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    /// <summary>
    /// GET /api/favorites - Get all favorite targets
    /// </summary>
    [Route(HttpVerbs.Get, "/favorites")]
    public async Task<List<FavoriteTarget>> GetFavoriteTargets()
    {
        try
        {
            if (!File.Exists(FavoritesFilePath)) return new List<FavoriteTarget>();

            var json = await File.ReadAllTextAsync(FavoritesFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return new List<FavoriteTarget>();
        }
    }

    /// <summary>
    /// DELETE /api/favorites/{id} - Delete a favorite target
    /// </summary>
    [Route(HttpVerbs.Delete, "/favorites/{id}")]
    public async Task<ApiResponse> DeleteFavoriteTarget(Guid id)
    {
        try
        {
            if (!File.Exists(FavoritesFilePath))
            {
                return new ApiResponse { Success = false, Error = "Keine Daten vorhanden", StatusCode = 404 };
            }

            var json = await File.ReadAllTextAsync(FavoritesFilePath);
            var favorites = System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);

            var toRemove = favorites.FirstOrDefault(f => f.Id == id);
            if (toRemove == null)
            {
                return new ApiResponse { Success = false, Error = "Eintrag nicht gefunden", StatusCode = 404 };
            }

            favorites.Remove(toRemove);
            await File.WriteAllTextAsync(FavoritesFilePath, System.Text.Json.JsonSerializer.Serialize(favorites, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            return new ApiResponse { Success = true, Response = $"Favorit mit Id '{id}' gelöscht", StatusCode = 200 };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse { Success = false, Error = ex.Message, StatusCode = 500 };
        }
    }

    /// <summary>
    /// PUT /api/favorites/{id} - Update a favorite target
    /// </summary>
    [Route(HttpVerbs.Put, "/favorites/{id}")]
    public async Task<ApiResponse> UpdateFavoriteTarget(Guid id)
    {
        try
        {
            var updatedTarget = await HttpContext.GetRequestDataAsync<FavoriteTarget>();

            if (!File.Exists(FavoritesFilePath))
            {
                return new ApiResponse { Success = false, Error = "Keine Daten vorhanden", StatusCode = 404 };
            }

            var json = await File.ReadAllTextAsync(FavoritesFilePath);
            var favorites = System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);

            var existing = favorites.FirstOrDefault(f => f.Id == id);
            if (existing == null)
            {
                return new ApiResponse { Success = false, Error = "Eintrag nicht gefunden", StatusCode = 404 };
            }

            existing.Name = updatedTarget.Name;
            existing.Ra = updatedTarget.Ra;
            existing.Dec = updatedTarget.Dec;
            existing.DecString = updatedTarget.DecString;
            existing.RaString = updatedTarget.RaString;
            existing.Rotation = updatedTarget.Rotation;

            await File.WriteAllTextAsync(FavoritesFilePath, System.Text.Json.JsonSerializer.Serialize(favorites, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            return new ApiResponse { Success = true, Response = existing, StatusCode = 200 };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse { Success = false, Error = ex.Message, StatusCode = 500 };
        }
    }
}
