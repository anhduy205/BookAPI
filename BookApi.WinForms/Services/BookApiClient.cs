using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookApi.WinForms.Models;

namespace BookApi.WinForms.Services;

public sealed class BookApiClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public BookApiClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9999/")
        };
    }

    public async Task<IReadOnlyList<CategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _httpClient.GetFromJsonAsync<List<CategoryItem>>("api/categories", cancellationToken);
        return categories ?? [];
    }

    public async Task<IReadOnlyList<BookItem>> SearchBooksAsync(string? keyword, int? categoryId, CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            queryParts.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        }

        if (categoryId.HasValue)
        {
            queryParts.Add($"categoryId={categoryId.Value}");
        }

        var requestUri = "api/books";
        if (queryParts.Count > 0)
        {
            requestUri += "?" + string.Join("&", queryParts);
        }

        var books = await _httpClient.GetFromJsonAsync<List<BookItem>>(requestUri, cancellationToken);
        return books ?? [];
    }

    public async Task<BookItem?> GetBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<BookItem>($"api/books/{bookId}", cancellationToken);
    }

    public async Task<BookItem> CreateBookAsync(BookFormModel model, string? imageFilePath, CancellationToken cancellationToken = default)
    {
        using var content = CreateMultipartContent(model, imageFilePath);
        using var response = await _httpClient.PostAsync("api/books", content, cancellationToken);
        return await ReadResponseAsync<BookItem>(response, cancellationToken);
    }

    public async Task<BookItem> UpdateBookAsync(int bookId, BookFormModel model, string? imageFilePath, CancellationToken cancellationToken = default)
    {
        using var content = CreateMultipartContent(model, imageFilePath);
        using var response = await _httpClient.PutAsync($"api/books/{bookId}", content, cancellationToken);
        return await ReadResponseAsync<BookItem>(response, cancellationToken);
    }

    public async Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/books/{bookId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<byte[]> GetImageBytesAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return _httpClient.GetByteArrayAsync(imageUrl, cancellationToken);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static MultipartFormDataContent CreateMultipartContent(BookFormModel model, string? imageFilePath)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(model.Title), "title");
        content.Add(new StringContent(model.Author), "author");
        content.Add(new StringContent(model.Price.ToString(CultureInfo.InvariantCulture)), "price");
        content.Add(new StringContent(model.Quantity.ToString(CultureInfo.InvariantCulture)), "quantity");
        content.Add(new StringContent(model.CategoryId.ToString(CultureInfo.InvariantCulture)), "categoryId");
        content.Add(new StringContent(model.Description ?? string.Empty), "description");

        if (!string.IsNullOrWhiteSpace(imageFilePath) && File.Exists(imageFilePath))
        {
            var stream = File.OpenRead(imageFilePath);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(imageFilePath));
            content.Add(streamContent, "imageFile", Path.GetFileName(imageFilePath));
        }

        return content;
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"Request failed with status code {(int)response.StatusCode}.";
        }

        throw new InvalidOperationException(message);
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
