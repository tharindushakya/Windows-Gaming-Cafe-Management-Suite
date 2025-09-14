using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;
using GamingCafe.Core.Models;

namespace GamingCafe.POS;

    public class SelectCustomerDialog : Window
{
    private GamingCafeContext _context = null!;
    private ListView _listView = null!;
    public int? SelectedCustomerId { get; private set; }

        private class CustomerListItem
        {
            public int UserId { get; set; }
            public string Display { get; set; } = string.Empty;
        }

    public SelectCustomerDialog(GamingCafeContext context)
    {
        _context = context;
        Title = "Select Customer";
        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

    var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
    var searchBox = new TextBox { Width = 300, Margin = new Thickness(0,0,10,0) };
    var searchBtn = new Button { Content = "Search", Padding = new Thickness(8,4,8,4) };
    searchPanel.Children.Add(searchBox);
    searchPanel.Children.Add(searchBtn);

    var pagingPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10,0,0,0), HorizontalAlignment = HorizontalAlignment.Right };
    var prevBtn = new Button { Content = "Prev", Padding = new Thickness(8,4,8,4), Margin = new Thickness(10,0,0,0) };
    var pageLabel = new TextBlock { Text = "Page 1", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10,0,0,0) };
    var nextBtn = new Button { Content = "Next", Padding = new Thickness(8,4,8,4), Margin = new Thickness(10,0,0,0) };
    pagingPanel.Children.Add(prevBtn);
    pagingPanel.Children.Add(pageLabel);
    pagingPanel.Children.Add(nextBtn);

    var headerPanel = new DockPanel { Margin = new Thickness(0) };
    headerPanel.Children.Add(searchPanel);
    DockPanel.SetDock(pagingPanel, Dock.Right);
    headerPanel.Children.Add(pagingPanel);
    Grid.SetRow(headerPanel, 0);
    grid.Children.Add(headerPanel);

        _listView = new ListView { Margin = new Thickness(10) };
        Grid.SetRow(_listView, 1);
        grid.Children.Add(_listView);

    var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10) };
        var selectBtn = new Button { Content = "Select", Padding = new Thickness(10), Margin = new Thickness(0,0,10,0) };
        var cancelBtn = new Button { Content = "Cancel", Padding = new Thickness(10) };
        btnPanel.Children.Add(selectBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 2);
        grid.Children.Add(btnPanel);

        Content = grid;

        int currentPage = 0;
        int pageSize = Settings.Load().UserQueryPageSize;

        pageLabel.Text = $"Page {currentPage + 1}";

        prevBtn.Click += async (s, e) =>
        {
            if (currentPage > 0) { currentPage--; pageLabel.Text = $"Page {currentPage + 1}"; await LoadCustomersRawAsync(searchBox.Text?.Trim(), currentPage, pageSize); }
        };

        nextBtn.Click += async (s, e) =>
        {
            currentPage++; pageLabel.Text = $"Page {currentPage + 1}"; await LoadCustomersRawAsync(searchBox.Text?.Trim(), currentPage, pageSize);
        };

        searchBtn.Click += async (s, e) =>
        {
            var q = searchBox.Text?.Trim() ?? string.Empty;
            try
            {
                currentPage = 0;
                pageLabel.Text = $"Page {currentPage + 1}";
                await LoadCustomersRawAsync(q, currentPage, pageSize);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show($"Error searching customers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        selectBtn.Click += (s, e) =>
        {
            if (_listView.SelectedItem is CustomerListItem item)
            {
                SelectedCustomerId = item.UserId;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select a customer.", "Select Customer", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        cancelBtn.Click += (s, e) => DialogResult = false;

    // initial load
    Loaded += async (s, e) =>
        {
            try
            {
        currentPage = 0;
        pageLabel.Text = $"Page {currentPage + 1}";
        await LoadCustomersRawAsync(null, currentPage, pageSize);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show($"Error loading customers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

    }

    private async Task LoadCustomersRawAsync(string? q)
    {
        await LoadCustomersRawAsync(q, 0, Settings.Load().UserQueryPageSize);
    }

    private async Task LoadCustomersRawAsync(string? q, int page, int pageSize)
    {
        // Defaults
        string tableName = "Users";
        string? schema = null;
        string userIdCol = "UserId";
        string usernameCol = "Username";
        string emailCol = "Email";

        var s = Settings.Load();
        if (!string.IsNullOrWhiteSpace(s.UserIdColumnName)) userIdCol = s.UserIdColumnName;
        if (!string.IsNullOrWhiteSpace(s.UsernameColumnName)) usernameCol = s.UsernameColumnName;
        if (!string.IsNullOrWhiteSpace(s.EmailColumnName)) emailCol = s.EmailColumnName;
        if (pageSize <= 0) pageSize = s.UserQueryPageSize;

        var correlationId = Logger.CreateCorrelationId();
        Logger.Log($"Starting customer load (q='{q}', page={page}, pageSize={pageSize})", correlationId);

    var results = new List<CustomerListItem>();
            // Prefer EF metadata for table/column mapping when available
            try
            {
                var entityType = _context.Model.FindEntityType(typeof(User));
                if (entityType != null)
                {
                    tableName = entityType.GetTableName() ?? tableName;
                    schema = entityType.GetSchema();
                    var store = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(tableName, schema);

                    var pId = entityType.FindProperty(nameof(User.UserId));
                    var pUser = entityType.FindProperty(nameof(User.Username));
                    var pEmail = entityType.FindProperty(nameof(User.Email));

                    if (pId != null && string.IsNullOrWhiteSpace(s.UserIdColumnName)) userIdCol = pId.GetColumnName(store) ?? userIdCol;
                    if (pUser != null && string.IsNullOrWhiteSpace(s.UsernameColumnName)) usernameCol = pUser.GetColumnName(store) ?? usernameCol;
                    if (pEmail != null && string.IsNullOrWhiteSpace(s.EmailColumnName)) emailCol = pEmail.GetColumnName(store) ?? emailCol;
                }
            }
            catch (Exception ex)
            {
                // metadata reading may fail on some EF versions; continue to information_schema
                Logger.Log(ex, correlationId);
            }

            var conn = _context.Database.GetDbConnection();

            // Determine if query is a numeric id search
            int numericId = 0;
            var isNumericId = !string.IsNullOrWhiteSpace(q) && int.TryParse(q, out numericId);

            // Safely attempt to read the connection string. Some providers/hosts may throw when reading or opening.
            string? connString = null;
            try
            {
                connString = conn.ConnectionString;
            }
            catch (Exception ex)
            {
                Logger.Log($"Unable to read DbConnection.ConnectionString: {ex.Message}", correlationId);
                connString = null;
            }

            // If no connection string, or reading it failed, use EF in-memory approach
            if (string.IsNullOrWhiteSpace(connString))
            {
                Logger.Log("Connection string is empty or unreadable - falling back to EF in-memory user load.", correlationId);
                var users = await _context.Set<User>().AsNoTracking()
                    .Select(u => new { u.UserId, u.Username, u.Email })
                    .ToListAsync();

                var filtered = string.IsNullOrEmpty(q)
                    ? users
                    : (isNumericId
                        ? users.Where(x => x.UserId == numericId)
                        : users.Where(x => (!string.IsNullOrEmpty(x.Username) && x.Username.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) || (!string.IsNullOrEmpty(x.Email) && x.Email.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)));

                if (s.EnableUserPaging)
                {
                    filtered = filtered.Skip(page * pageSize).Take(pageSize);
                }

                var resultsLocal = filtered.Select(x => new CustomerListItem { UserId = x.UserId, Display = $"{x.Username} ({x.Email})" }).ToList();
                _listView.ItemsSource = resultsLocal;
                _listView.DisplayMemberPath = "Display";
                Logger.Log($"Loaded {resultsLocal.Count} customers from EF in-memory", correlationId);
                return;
            }

            // At this point we have a connection string — attempt provider-aware raw SQL. Guard any failure and fall back to EF in-memory.
            try
            {
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

                var provider = _context.Database.ProviderName ?? string.Empty;

                // Query information_schema.columns to discover real column names when necessary
                var cmdCols = conn.CreateCommand();
                cmdCols.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_name = @t";
                var p = cmdCols.CreateParameter();
                p.ParameterName = "@t";
                p.Value = provider.IndexOf("npgsql", StringComparison.OrdinalIgnoreCase) >= 0 ? tableName.ToLowerInvariant() : tableName;
                cmdCols.Parameters.Add(p);

                var availableCols = new List<string>();
                await using (var rdr = await cmdCols.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync()) availableCols.Add(rdr.GetString(0));
                }
            

            string PickBest(IEnumerable<string> candidates)
            {
                foreach (var c in candidates)
                {
                    var found = availableCols.FirstOrDefault(ac => string.Equals(ac, c, StringComparison.OrdinalIgnoreCase));
                    if (found != null) return found;
                }
                return candidates.First();
            }

            if (string.IsNullOrWhiteSpace(s.UserIdColumnName)) userIdCol = PickBest(new[] { "UserId", "user_id", "userid", "id" });
            if (string.IsNullOrWhiteSpace(s.UsernameColumnName)) usernameCol = PickBest(new[] { "Username", "username", "user_name", "name" });
            if (string.IsNullOrWhiteSpace(s.EmailColumnName)) emailCol = PickBest(new[] { "Email", "email", "email_address" });

            string sql;
            string quotedTable;
            if (provider.IndexOf("npgsql", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                quotedTable = string.IsNullOrEmpty(schema) ? $"\"{tableName}\"" : $"\"{schema}\".\"{tableName}\"";
                if (string.IsNullOrEmpty(q))
                {
                    sql = $"SELECT \"{userIdCol}\", \"{usernameCol}\", \"{emailCol}\" FROM {quotedTable} WHERE \"{usernameCol}\" IS NOT NULL LIMIT {pageSize}";
                }
                else if (isNumericId)
                {
                    sql = $"SELECT \"{userIdCol}\", \"{usernameCol}\", \"{emailCol}\" FROM {quotedTable} WHERE \"{userIdCol}\" = @p LIMIT {pageSize}";
                }
                else
                {
                    sql = $"SELECT \"{userIdCol}\", \"{usernameCol}\", \"{emailCol}\" FROM {quotedTable} WHERE (\"{usernameCol}\" ILIKE @p OR \"{emailCol}\" ILIKE @p) LIMIT {pageSize}";
                }
            }
            else if (provider.IndexOf("sqlserver", StringComparison.OrdinalIgnoreCase) >= 0 || provider.IndexOf("microsoft.entityframeworkcore.sqlserver", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                quotedTable = string.IsNullOrEmpty(schema) ? $"[{tableName}]" : $"[{schema}].[{tableName}]";
                if (string.IsNullOrEmpty(q))
                {
                    sql = $"SELECT TOP ({pageSize}) [{userIdCol}], [{usernameCol}], [{emailCol}] FROM {quotedTable} WHERE [{usernameCol}] IS NOT NULL";
                }
                else if (isNumericId)
                {
                    sql = $"SELECT TOP ({pageSize}) [{userIdCol}], [{usernameCol}], [{emailCol}] FROM {quotedTable} WHERE [{userIdCol}] = @p";
                }
                else
                {
                    // Use ORDER BY to allow OFFSET/FETCH if later used
                    sql = $"SELECT TOP ({pageSize}) [{userIdCol}], [{usernameCol}], [{emailCol}] FROM {quotedTable} WHERE ([{usernameCol}] LIKE @p OR [{emailCol}] LIKE @p)";
                }
            }
            else
            {
                // Unknown provider, fall back to EF in-memory
                Logger.Log($"Unknown EF provider '{provider}' - falling back to EF in-memory load.", correlationId);
                var users = await _context.Set<User>().AsNoTracking().Select(u => new { u.UserId, u.Username, u.Email }).ToListAsync();
                var filtered = string.IsNullOrEmpty(q) ? users : users.Where(x => (!string.IsNullOrEmpty(x.Username) && x.Username.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) || (!string.IsNullOrEmpty(x.Email) && x.Email.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
                if (s.EnableUserPaging) filtered = filtered.Skip(page * pageSize).Take(pageSize);
                var rlocal = filtered.Select(x => new CustomerListItem { UserId = x.UserId, Display = $"{x.Username} ({x.Email})" }).ToList();
                _listView.ItemsSource = rlocal;
                _listView.DisplayMemberPath = "Display";
                Logger.Log($"Loaded {rlocal.Count} customers from EF in-memory (unknown provider)", correlationId);
                return;
            }

            var cmd2 = conn.CreateCommand();
            cmd2.CommandText = sql;
            if (!string.IsNullOrEmpty(q))
            {
                var p2 = cmd2.CreateParameter();
                p2.ParameterName = "@p";
                p2.Value = isNumericId ? (object)numericId : (object)$"%{q}%";
                cmd2.Parameters.Add(p2);
            }

            // For Postgres add OFFSET when paging
            if (s.EnableUserPaging && provider.IndexOf("npgsql", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var offset = page * pageSize;
                cmd2.CommandText += $" OFFSET {offset}";
            }
            else if (s.EnableUserPaging && provider.IndexOf("sqlserver", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // SQL Server: OFFSET requires ORDER BY; complex handling skipped for now.
            }

            await using (var reader = await cmd2.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                    var uname = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    var mail = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    results.Add(new CustomerListItem { UserId = id, Display = $"{uname} ({mail})" });
                }
            }

                _listView.ItemsSource = results;
                _listView.DisplayMemberPath = "Display";
                Logger.Log($"Loaded {results.Count} customers via raw SQL", correlationId);
            }
            catch (Exception ex)
            {
                // If any error happens during raw SQL path, fallback to EF in-memory approach and log the exception.
                Logger.Log(ex, correlationId);
                try
                {
                    var users = await _context.Set<User>().AsNoTracking().Select(u => new { u.UserId, u.Username, u.Email }).ToListAsync();
                    var filtered = string.IsNullOrEmpty(q)
                        ? users
                        : (isNumericId ? users.Where(x => x.UserId == numericId) : users.Where(x => (!string.IsNullOrEmpty(x.Username) && x.Username.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) || (!string.IsNullOrEmpty(x.Email) && x.Email.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)));
                    if (s.EnableUserPaging) filtered = filtered.Skip(page * pageSize).Take(pageSize);
                    var rlocal = filtered.Select(x => new CustomerListItem { UserId = x.UserId, Display = $"{x.Username} ({x.Email})" }).ToList();
                    _listView.ItemsSource = rlocal;
                    _listView.DisplayMemberPath = "Display";
                    Logger.Log($"Loaded {rlocal.Count} customers from EF in-memory (fallback)", correlationId);
                }
                catch (Exception inner)
                {
                    Logger.Log(inner, correlationId);
                }
            }
            finally
            {
                try { if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open) await _context.Database.GetDbConnection().CloseAsync(); } catch { }
            }
    }
}
