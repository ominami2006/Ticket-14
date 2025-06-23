using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
namespace Ticket_14 {
    public partial class EditForm : Form {
        private readonly Dictionary<string, TextBox> _textBoxes = new Dictionary<string, TextBox>();
        private readonly List<string> _readOnlyColumns;
        private readonly DataTable _table;
        public Dictionary<string, object> RowData { get; private set; }
        private DataRow _rowToEdit;
        private List<string> _pkColumns;
        public EditForm(DataTable table, DataRow rowToEdit = null, List<string> pkColumns = null) {
            InitializeComponent();
            _pkColumns = pkColumns ?? new List<string>();
            _readOnlyColumns = _pkColumns;
            _rowToEdit = rowToEdit; 
            _table = table;
        }
        private void EditForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (this.DialogResult == DialogResult.OK) {
                RowData.Clear();
                foreach (var kvp in _textBoxes) {
                    if (!kvp.Value.ReadOnly) {
                        var column = _table.Columns[kvp.Key];
                        object value = kvp.Value.Text;
                        try {
                            if (string.IsNullOrWhiteSpace(kvp.Value.Text))
                                value = DBNull.Value;
                            else
                                value = Convert.ChangeType(kvp.Value.Text, column.DataType);
                        } catch {
                            MessageBox.Show($"Ошибка в поле '{kvp.Key}': не удалось преобразовать '{kvp.Value.Text}' к типу {column.DataType.Name}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            e.Cancel = true;
                            return;
                        }
                        RowData[kvp.Key] = value;
                    }
                }
            }
        }
        private void EditForm_Load(object sender, EventArgs e) {
            this.Text = _rowToEdit == null ? "Add New Row" : "Edit Row";
            RowData = new Dictionary<string, object>();
            int yPos = 15;
            foreach (DataColumn column in _table.Columns) {
                if (column.AutoIncrement && _rowToEdit == null)
                    continue;
                var label = new Label {
                    Text = column.ColumnName,
                    Location = new Point(15, yPos),
                    AutoSize = true
                };
                this.Controls.Add(label);
                var textBox = new TextBox {
                    Name = "txt" + column.ColumnName,
                    Location = new Point(200, yPos - 3),
                    Size = new Size(250, 20)
                };
                if (_rowToEdit != null)
                    textBox.Text = _rowToEdit[column.ColumnName]?.ToString() ?? "";
                if (_readOnlyColumns.Contains(column.ColumnName) || column.AutoIncrement) {
                    textBox.ReadOnly = true;
                    textBox.BackColor = SystemColors.ControlLight;
                }
                this.Controls.Add(textBox);
                _textBoxes.Add(column.ColumnName, textBox);
                yPos += 30;
            }
            var btnSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(15, yPos + 10), Size = new Size(150, 40) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(175, yPos + 10), Size = new Size(150, 40) };
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
            this.Height = yPos + 100;
            this.Width = 500;
        }
    }
}
