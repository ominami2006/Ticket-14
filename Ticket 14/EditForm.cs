using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Ticket_14
{
    public partial class EditForm : Form
    {
        private readonly Dictionary<string, TextBox> _textBoxes = new Dictionary<string, TextBox>();
        private readonly List<string> _readOnlyColumns;
        public Dictionary<string, object> RowData { get; private set; }
        public EditForm(DataTable table, DataRow rowToEdit = null, List<string> pkColumns = null)
        {
            InitializeComponent();
            _readOnlyColumns = pkColumns ?? new List<string>();
            RowData = new Dictionary<string, object>();

            this.Text = rowToEdit == null ? "Add New Row" : "Edit Row";


            int yPos = 15;
            foreach (DataColumn column in table.Columns)
            {
                if (column.AutoIncrement && rowToEdit == null)
                {
                    continue;
                }
                var label = new Label
                {
                    Text = column.ColumnName,
                    Location = new Point(15, yPos),
                    AutoSize = true
                };
                this.Controls.Add(label);
                var textBox = new TextBox
                {
                    Name = "txt" + column.ColumnName,
                    Location = new Point(160, yPos - 3),
                    Size = new Size(250, 20)
                };

                if (rowToEdit != null)
                {
                    textBox.Text = rowToEdit[column.ColumnName]?.ToString() ?? "";
                }

                if (_readOnlyColumns.Contains(column.ColumnName) || column.AutoIncrement)
                {
                    textBox.ReadOnly = true;
                    textBox.BackColor = SystemColors.ControlLight;
                }

                this.Controls.Add(textBox);
                _textBoxes.Add(column.ColumnName, textBox);

                yPos += 30; 
            }

            var btnSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(254, yPos + 10) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(335, yPos + 10) };

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;

            this.Height = yPos + 80;
        }

        private void EditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                foreach (var kvp in _textBoxes)
                {
                    if (!kvp.Value.ReadOnly)
                    {
                        RowData.Add(kvp.Key, kvp.Value.Text);
                    }
                }
            }
        }
    }
}
