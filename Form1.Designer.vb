<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Label1 = New Label()
        cboBizCode = New ComboBox()
        Label2 = New Label()
        dtpDate = New DateTimePicker()
        Label3 = New Label()
        txtBankAmount = New TextBox()
        btnCheck = New Button()
        btnSearch = New Button()
        dgvIncome = New DataGridView()
        CType(dgvIncome, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(21, 21)
        Label1.Name = "Label1"
        Label1.Size = New Size(55, 15)
        Label1.TabIndex = 0
        Label1.Text = "업장코드"
        ' 
        ' cboBizCode
        ' 
        cboBizCode.FormattingEnabled = True
        cboBizCode.Location = New Point(81, 19)
        cboBizCode.Name = "cboBizCode"
        cboBizCode.Size = New Size(132, 23)
        cboBizCode.TabIndex = 1
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(219, 21)
        Label2.Name = "Label2"
        Label2.Size = New Size(55, 15)
        Label2.TabIndex = 0
        Label2.Text = "수입일자"
        ' 
        ' dtpDate
        ' 
        dtpDate.Format = DateTimePickerFormat.Short
        dtpDate.Location = New Point(280, 19)
        dtpDate.Name = "dtpDate"
        dtpDate.Size = New Size(107, 23)
        dtpDate.TabIndex = 2
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(428, 22)
        Label3.Name = "Label3"
        Label3.Size = New Size(67, 15)
        Label3.TabIndex = 0
        Label3.Text = "통장입금액"
        ' 
        ' txtBankAmount
        ' 
        txtBankAmount.Location = New Point(501, 21)
        txtBankAmount.Name = "txtBankAmount"
        txtBankAmount.Size = New Size(113, 23)
        txtBankAmount.TabIndex = 3
        ' 
        ' btnCheck
        ' 
        btnCheck.Location = New Point(620, 21)
        btnCheck.Name = "btnCheck"
        btnCheck.Size = New Size(75, 23)
        btnCheck.TabIndex = 4
        btnCheck.Text = "체크"
        btnCheck.UseVisualStyleBackColor = True
        ' 
        ' btnSearch
        ' 
        btnSearch.Location = New Point(701, 21)
        btnSearch.Name = "btnSearch"
        btnSearch.Size = New Size(75, 23)
        btnSearch.TabIndex = 4
        btnSearch.Text = "조회"
        btnSearch.UseVisualStyleBackColor = True
        ' 
        ' dgvIncome
        ' 
        dgvIncome.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvIncome.Location = New Point(21, 70)
        dgvIncome.Name = "dgvIncome"
        dgvIncome.Size = New Size(873, 285)
        dgvIncome.TabIndex = 5
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(914, 373)
        Controls.Add(dgvIncome)
        Controls.Add(btnSearch)
        Controls.Add(btnCheck)
        Controls.Add(txtBankAmount)
        Controls.Add(dtpDate)
        Controls.Add(cboBizCode)
        Controls.Add(Label3)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "일일수입내역 조회"
        CType(dgvIncome, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents cboBizCode As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents dtpDate As DateTimePicker
    Friend WithEvents Label3 As Label
    Friend WithEvents txtBankAmount As TextBox
    Friend WithEvents btnCheck As Button
    Friend WithEvents btnSearch As Button
    Friend WithEvents dgvIncome As DataGridView

End Class
