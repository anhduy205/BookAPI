using BookApi.WinForms.Models;
using BookApi.WinForms.Services;

namespace BookApi.WinForms;

public partial class Form1 : Form
{
    private readonly BookApiClient _apiClient = new();
    private readonly LocalApiHost _apiHost = new();
    private bool _suppressSelectionChanged;
    private string? _selectedImageFilePath;
    private int? _selectedBookId;

    public Form1()
    {
        InitializeComponent();
        HookEvents();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await InitializeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _apiHost.Dispose();
            _apiClient.Dispose();
            components?.Dispose();
            if (picBook.Image is not null)
            {
                picBook.Image.Dispose();
                picBook.Image = null;
            }
        }

        base.Dispose(disposing);
    }

    private void HookEvents()
    {
        btnSearch.Click += async (_, _) => await SearchBooksAsync();
        btnRefresh.Click += async (_, _) => await RefreshSearchAsync();
        btnChooseImage.Click += (_, _) => ChooseImage();
        btnAdd.Click += async (_, _) => await AddBookAsync();
        btnUpdate.Click += async (_, _) => await UpdateBookAsync();
        btnDelete.Click += async (_, _) => await DeleteBookAsync();
        btnClear.Click += (_, _) => ClearForm(clearGridSelection: true);
        dgvBooks.SelectionChanged += async (_, _) => await HandleSelectionChangedAsync();
        txtKeyword.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SearchBooksAsync();
            }
        };
    }

    private async Task InitializeAsync()
    {
        try
        {
            UseWaitCursor = true;
            await _apiHost.EnsureAvailableAsync();
            await LoadCategoriesAsync();
            await SearchBooksAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _apiClient.GetCategoriesAsync();
        var categoryItems = categories
            .Select(category => new CategoryOption
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName
            })
            .ToList();

        cboCategory.DataSource = categoryItems.ToList();
        cboCategory.DisplayMember = nameof(CategoryOption.CategoryName);

        var searchItems = new List<CategoryOption>
        {
            new() { CategoryId = null, CategoryName = "All Categories" }
        };
        searchItems.AddRange(categoryItems);

        cboSearchCategory.DataSource = searchItems;
        cboSearchCategory.DisplayMember = nameof(CategoryOption.CategoryName);
        if (cboSearchCategory.Items.Count > 0)
        {
            cboSearchCategory.SelectedIndex = 0;
        }

        if (cboCategory.Items.Count > 0)
        {
            cboCategory.SelectedIndex = 0;
        }
    }

    private async Task RefreshSearchAsync()
    {
        txtKeyword.Clear();
        if (cboSearchCategory.Items.Count > 0)
        {
            cboSearchCategory.SelectedIndex = 0;
        }

        await SearchBooksAsync();
    }

    private async Task SearchBooksAsync(int? selectBookId = null)
    {
        try
        {
            SetBusy(true);
            var books = await _apiClient.SearchBooksAsync(txtKeyword.Text, GetSelectedSearchCategoryId());
            _suppressSelectionChanged = true;
            dgvBooks.DataSource = books.ToList();
            dgvBooks.ClearSelection();
            _suppressSelectionChanged = false;

            if (selectBookId.HasValue)
            {
                await SelectBookInGridAsync(selectBookId.Value);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task HandleSelectionChangedAsync()
    {
        if (_suppressSelectionChanged)
        {
            return;
        }

        if (dgvBooks.CurrentRow?.DataBoundItem is not BookItem book)
        {
            return;
        }

        if (_selectedBookId == book.BookId)
        {
            return;
        }

        try
        {
            var detail = await _apiClient.GetBookAsync(book.BookId);
            if (detail is not null)
            {
                await PopulateFormAsync(detail);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task PopulateFormAsync(BookItem book)
    {
        _selectedBookId = book.BookId;
        _selectedImageFilePath = null;
        txtBookId.Text = book.BookId.ToString();
        txtTitle.Text = book.Title;
        txtAuthor.Text = book.Author;
        nudPrice.Value = Math.Min(nudPrice.Maximum, book.Price);
        nudQuantity.Value = Math.Min(nudQuantity.Maximum, book.Quantity);
        txtDescription.Text = book.Description ?? string.Empty;
        txtImagePath.Text = book.ImagePath ?? string.Empty;
        SelectCategory(book.CategoryId);
        await LoadPreviewAsync(book.ImageUrl);
    }

    private void ChooseImage()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp",
            Title = "Choose a book image"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _selectedImageFilePath = dialog.FileName;
        txtImagePath.Text = dialog.FileName;
        SetPreviewImage(LoadImageFromFile(dialog.FileName));
    }

    private async Task AddBookAsync()
    {
        var formModel = BuildFormModel();
        if (formModel is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var createdBook = await _apiClient.CreateBookAsync(formModel, _selectedImageFilePath);
            MessageBox.Show(this, "Book added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await SearchBooksAsync(createdBook.BookId);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task UpdateBookAsync()
    {
        if (!_selectedBookId.HasValue)
        {
            MessageBox.Show(this, "Please select a book to update.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var formModel = BuildFormModel();
        if (formModel is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var updatedBook = await _apiClient.UpdateBookAsync(_selectedBookId.Value, formModel, _selectedImageFilePath);
            MessageBox.Show(this, "Book updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await SearchBooksAsync(updatedBook.BookId);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task DeleteBookAsync()
    {
        if (!_selectedBookId.HasValue)
        {
            MessageBox.Show(this, "Please select a book to delete.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(this, "Delete the selected book?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.DeleteBookAsync(_selectedBookId.Value);
            MessageBox.Show(this, "Book deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm(clearGridSelection: true);
            await SearchBooksAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private BookFormModel? BuildFormModel()
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show(this, "Title is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtTitle.Focus();
            return null;
        }

        if (string.IsNullOrWhiteSpace(txtAuthor.Text))
        {
            MessageBox.Show(this, "Author is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtAuthor.Focus();
            return null;
        }

        if (cboCategory.SelectedItem is not CategoryOption category || !category.CategoryId.HasValue)
        {
            MessageBox.Show(this, "Please choose a category.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cboCategory.Focus();
            return null;
        }

        return new BookFormModel
        {
            Title = txtTitle.Text.Trim(),
            Author = txtAuthor.Text.Trim(),
            Price = nudPrice.Value,
            Quantity = decimal.ToInt32(nudQuantity.Value),
            Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
            CategoryId = category.CategoryId.Value
        };
    }

    private int? GetSelectedSearchCategoryId()
    {
        return (cboSearchCategory.SelectedItem as CategoryOption)?.CategoryId;
    }

    private void SelectCategory(int categoryId)
    {
        for (var index = 0; index < cboCategory.Items.Count; index++)
        {
            if (cboCategory.Items[index] is CategoryOption option && option.CategoryId == categoryId)
            {
                cboCategory.SelectedIndex = index;
                return;
            }
        }
    }

    private async Task LoadPreviewAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            SetPreviewImage(null);
            return;
        }

        try
        {
            var bytes = await _apiClient.GetImageBytesAsync(imageUrl);
            SetPreviewImage(LoadImageFromBytes(bytes));
        }
        catch
        {
            SetPreviewImage(null);
        }
    }

    private void SetPreviewImage(Image? image)
    {
        if (picBook.Image is not null)
        {
            picBook.Image.Dispose();
        }

        picBook.Image = image;
    }

    private static Image LoadImageFromFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static Image LoadImageFromBytes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private async Task SelectBookInGridAsync(int bookId)
    {
        foreach (DataGridViewRow row in dgvBooks.Rows)
        {
            if (row.DataBoundItem is BookItem book && book.BookId == bookId)
            {
                _suppressSelectionChanged = true;
                row.Selected = true;
                dgvBooks.CurrentCell = row.Cells[0];
                _suppressSelectionChanged = false;
                var detail = await _apiClient.GetBookAsync(book.BookId);
                if (detail is not null)
                {
                    await PopulateFormAsync(detail);
                }
                return;
            }
        }
    }

    private void ClearForm(bool clearGridSelection)
    {
        _selectedBookId = null;
        _selectedImageFilePath = null;
        txtBookId.Clear();
        txtTitle.Clear();
        txtAuthor.Clear();
        nudPrice.Value = 0;
        nudQuantity.Value = 0;
        txtDescription.Clear();
        txtImagePath.Clear();
        if (cboCategory.Items.Count > 0)
        {
            cboCategory.SelectedIndex = 0;
        }

        SetPreviewImage(null);

        if (clearGridSelection)
        {
            _suppressSelectionChanged = true;
            dgvBooks.ClearSelection();
            _suppressSelectionChanged = false;
        }
    }

    private void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;
        btnSearch.Enabled = !isBusy;
        btnRefresh.Enabled = !isBusy;
        btnAdd.Enabled = !isBusy;
        btnUpdate.Enabled = !isBusy;
        btnDelete.Enabled = !isBusy;
        btnClear.Enabled = !isBusy;
        btnChooseImage.Enabled = !isBusy;
    }

    private void ShowError(Exception ex)
    {
        MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
