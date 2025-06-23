using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Ticket_14 {
    public partial class MainForm : Form {
        private DatabaseService _dbService;
        private string _selectedTable;
        private List<string> _currentPrimaryKeys;
        private string connectionString = "Server=localhost;Database=ticket14;Trusted_Connection=True;";
        public MainForm() {
            InitializeComponent();
        }
        private void MainForm_Shown(object sender, EventArgs e) {
            try {
                _dbService?.Dispose();
                _dbService = new DatabaseService(connectionString);
                List<string> tableNames = _dbService.GetTableNames();
                lstTables.DataSource = tableNames;
                dataGridView.DataSource = null;
                btnEdit.Enabled = false;
                btnDelete.Enabled = false;
                btnAdd.Enabled = false;
            } catch (Exception ex) {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _dbService?.Dispose();
                _dbService = null;
            }
        }
        private void lstTables_SelectedIndexChanged(object sender, EventArgs e) {
            if (lstTables.SelectedItem == null || _dbService == null)
                return;
            _selectedTable = lstTables.SelectedItem.ToString();
            LoadTableData();
        }
        private void LoadTableData() {
            try {
                DataTable data = _dbService.GetTableData(_selectedTable);
                _currentPrimaryKeys = _dbService.GetPrimaryKeys(_selectedTable);
                dataGridView.DataSource = data;
                dataGridView.ReadOnly = true;
                dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView.AllowUserToAddRows = false;
                bool hasPrimaryKey = _currentPrimaryKeys.Any();
                btnAdd.Enabled = true;
                btnEdit.Enabled = hasPrimaryKey;
                btnDelete.Enabled = hasPrimaryKey;
                if (!hasPrimaryKey)
                    lblStatus.Text = "Warning: No primary key found. Editing and Deleting are disabled.";
                else
                    lblStatus.Text = $"Primary Key(s): {string.Join(", ", _currentPrimaryKeys)}";
            } catch (Exception ex) {
                MessageBox.Show($"Failed to load data for table '{_selectedTable}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnAdd_Click(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(_selectedTable) || dataGridView.DataSource == null) return;
            var tableSchema = (DataTable)dataGridView.DataSource;
            using (var form = new EditForm(tableSchema, null, _currentPrimaryKeys)) {
                if (form.ShowDialog() == DialogResult.OK) {
                    try {
                        var autoIncrementColumns = tableSchema.Columns
                            .Cast<DataColumn>()
                            .Where(c => c.AutoIncrement)
                            .Select(c => c.ColumnName)
                            .ToList();

                        _dbService.InsertRow(_selectedTable, form.RowData, autoIncrementColumns);
                        LoadTableData();
                    } catch (Exception ex) {
                        MessageBox.Show($"Failed to add row: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void btnEdit_Click(object sender, EventArgs e) {
            if (dataGridView.SelectedRows.Count == 0) return;
            var selectedRow = ((DataRowView)dataGridView.SelectedRows[0].DataBoundItem).Row;
            var tableSchema = (DataTable)dataGridView.DataSource;
            using (var form = new EditForm(tableSchema, selectedRow, _currentPrimaryKeys)) {
                if (form.ShowDialog() == DialogResult.OK) {
                    try {
                        var pkValues = new Dictionary<string, object>();
                        foreach (var key in _currentPrimaryKeys)
                            pkValues.Add(key, selectedRow[key]);
                        _dbService.UpdateRow(_selectedTable, form.RowData, pkValues);
                        LoadTableData();
                    } catch (Exception ex) {
                        MessageBox.Show($"Failed to update row: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void btnDelete_Click(object sender, EventArgs e) {
            if (dataGridView.SelectedRows.Count == 0) return;
            var result = MessageBox.Show("Are you sure you want to delete this row?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) {
                try {
                    var selectedRow = ((DataRowView)dataGridView.SelectedRows[0].DataBoundItem).Row;
                    var pkValues = new Dictionary<string, object>();
                    foreach (var key in _currentPrimaryKeys)
                        pkValues.Add(key, selectedRow[key]);
                    _dbService.DeleteRow(_selectedTable, pkValues);
                    LoadTableData();
                } catch (Exception ex) {
                    MessageBox.Show($"Failed to delete row: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            _dbService?.Dispose();
        }
    }
}
