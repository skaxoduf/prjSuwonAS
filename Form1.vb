Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient ' .NET 8.0 환경 

Public Class Form1
    Inherits Form

    ' 컨트롤 선언
    Private pnlTop As Panel
    Private lblTitle As Label
    Private WithEvents cboBizCode As ComboBox
    Private WithEvents dtpDate As DateTimePicker
    Private WithEvents txtBankAmount As TextBox
    Private WithEvents btnCheck As Button
    Private WithEvents btnSearch As Button
    Private dgvIncome As DataGridView

    Public Sub New()
        ' 폼 초기화 설정
        Me.Text = "일일수입내역 조회"
        Me.Size = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("맑은 고딕", 9) ' 가독성을 위해 폰트 변경

        InitializeCustomUI()
    End Sub

    ' UI 동적 생성 및 레이아웃 배치 (디자이너 없이 코드로 UI 구현)
    Private Sub InitializeCustomUI()
        ' 1. 상단 패널 (검색 조건 영역)
        pnlTop = New Panel()
        pnlTop.Dock = DockStyle.Top
        pnlTop.Height = 80
        pnlTop.BackColor = Color.WhiteSmoke
        Me.Controls.Add(pnlTop)

        ' 2. 타이틀 라벨
        lblTitle = New Label()
        lblTitle.Text = "일일수입관리 조회"
        lblTitle.Font = New Font("맑은 고딕", 12, FontStyle.Bold)
        lblTitle.Location = New Point(12, 12)
        lblTitle.AutoSize = True
        pnlTop.Controls.Add(lblTitle)

        ' 3. 업장코드 (라벨 + 콤보박스)
        Dim lblBiz As New Label() With {.Text = "업장코드", .Location = New Point(12, 45), .AutoSize = True}
        cboBizCode = New ComboBox() With {
            .Location = New Point(75, 42),
            .Width = 100,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cboBizCode.Items.Add("17110002") ' 예시 데이터
        cboBizCode.SelectedIndex = 0
        pnlTop.Controls.Add(lblBiz)
        pnlTop.Controls.Add(cboBizCode)

        ' 4. 수입일자 (라벨 + DateTimePicker) -> VB6의 콤보박스보다 훨씬 편합니다
        Dim lblDate As New Label() With {.Text = "수입일자", .Location = New Point(190, 45), .AutoSize = True}
        dtpDate = New DateTimePicker() With {
            .Location = New Point(250, 42),
            .Width = 120,
            .Format = DateTimePickerFormat.Short
        }
        pnlTop.Controls.Add(lblDate)
        pnlTop.Controls.Add(dtpDate)

        ' 5. 조회 버튼 (오른쪽 정렬)
        btnSearch = New Button() With {
            .Text = "조회",
            .Size = New Size(80, 30),
            .Location = New Point(pnlTop.Width - 100, 38),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .BackColor = Color.AliceBlue
        }
        pnlTop.Controls.Add(btnSearch)

        ' 6. 체크 버튼 (오른쪽 정렬)
        btnCheck = New Button() With {
            .Text = "체크",
            .Size = New Size(80, 30),
            .Location = New Point(pnlTop.Width - 190, 38),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        pnlTop.Controls.Add(btnCheck)

        ' 7. 통장입금액 (라벨 + 텍스트박스)
        Dim lblBank As New Label() With {
            .Text = "통장입금액",
            .Location = New Point(pnlTop.Width - 360, 45),
            .AutoSize = True,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        txtBankAmount = New TextBox() With {
            .Location = New Point(pnlTop.Width - 290, 42),
            .Width = 90,
            .TextAlign = HorizontalAlignment.Right,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        pnlTop.Controls.Add(lblBank)
        pnlTop.Controls.Add(txtBankAmount)


        ' 8. 메인 그리드 (DataGridView) 설정
        dgvIncome = New DataGridView()
        dgvIncome.Dock = DockStyle.Fill
        dgvIncome.BackgroundColor = Color.LightGray
        dgvIncome.AllowUserToAddRows = False ' 빈 행 추가 방지
        dgvIncome.RowHeadersVisible = True   ' VB6 느낌을 위해 행 헤더 표시
        dgvIncome.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.Controls.Add(dgvIncome)

        ' 그리드 컬럼 초기화 (화면에 보이는 A,B,C,D,E 대신 실제 비즈니스 컬럼명으로 추정하여 구성)
        SetupGridColumns()
    End Sub

    Private Sub SetupGridColumns()
        dgvIncome.Columns.Clear()

        ' 컬럼 추가 (Name, HeaderText, Width)
        dgvIncome.Columns.Add("No", "No")
        dgvIncome.Columns.Add("IncomeType", "수입항목")
        dgvIncome.Columns.Add("SystemAmt", "전산수입액")
        dgvIncome.Columns.Add("BankAmt", "실입금액")
        dgvIncome.Columns.Add("DiffAmt", "차액")
        dgvIncome.Columns.Add("Remark", "비고")

        ' 숫자 컬럼 우측 정렬 및 포맷팅
        With dgvIncome.Columns("SystemAmt").DefaultCellStyle
            .Format = "N0" ' 3자리 콤마
            .Alignment = DataGridViewContentAlignment.MiddleRight
        End With
        With dgvIncome.Columns("BankAmt").DefaultCellStyle
            .Format = "N0"
            .Alignment = DataGridViewContentAlignment.MiddleRight
        End With
        With dgvIncome.Columns("DiffAmt").DefaultCellStyle
            .Format = "N0"
            .Alignment = DataGridViewContentAlignment.MiddleRight
            .ForeColor = Color.Red ' 차액은 빨간색 강조
        End With

        ' 테스트용 더미 데이터 추가
        dgvIncome.Rows.Add("1", "카드매출", 500000, 500000, 0, "")
        dgvIncome.Rows.Add("2", "현금매출", 200000, 190000, -10000, "확인필요")

    End Sub

    ' 조회 버튼 클릭 이벤트
    Private Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click




        ' 1. 유효성 검사
        If cboBizCode.Text = "" Then
            MessageBox.Show("업장코드를 선택하세요.")
            Return
        End If

        Cursor = Cursors.WaitCursor
        dgvIncome.DataSource = Nothing ' 그리드 초기화

        ' 2. SQL문 작성 (VB6 쿼리 그대로 사용하되 파라미터화)
        Dim sql As String = "
            SELECT Hkno, Gojino, Sbdate, SHangmok, KigwanCd, BusoCd, HkGbCd, SSCode, SSCodename
                 , SUM(Cashamt) AS Cashamt
                 , SUM(Cardamt) AS Cardamt
                 , SUM(TotAmt) As TotAmt
                 , SUM(Koamt) AS Koamt
                 , SUM(VatAmt) As VatAmt
            FROM (
                SELECT T.Hkno, T.Gojino, T.Sbdate, T.Spdate, T.Skdate, T.Tjcode, T.AmtGb, T.Cashamt, T.Cardamt
                     , T.Totamt,  T.Koamt,  T.VatAmt, T.ChkData,  S.SHangmok, S.KigwanCd, S.BusoCd, S.HkGbCd
                     , S.SSCode, S.SSCodename
                FROM T_TRSINOUT AS T 
                LEFT OUTER JOIN T_SHCODE AS S ON T.AgentCode = S.AgentCode AND T.Hkno = S.Hkno
                WHERE T.AgentCode = @AgentCode
                  AND T.Isdel = 'N'
                  AND T.Sbdate = @SbDate
                  AND (T.AmtGb = '1' Or T.AmtGb = '2' Or T.AmtGb = '4' Or T.AmtGb = '5')
                  AND T.Tjcode = '1'  
                  AND T.Hkno <> ''
            ) AS K
            GROUP BY Hkno, Gojino, Sbdate, SHangmok, KigwanCd, BusoCd, HkGbCd, SSCode, SSCodename
            ORDER BY SSCode ASC
        "

        Try
            Using conn As SqlConnection = modDBConn.GetConnection()
                If conn Is Nothing Then Exit Sub ' 연결 실패 시 종료

                Dim cmd As New SqlCommand(sql, conn)
                ' 파라미터 바인딩 (SQL Injection 방지)
                cmd.Parameters.AddWithValue("@AgentCode", cboBizCode.Text.Trim())
                cmd.Parameters.AddWithValue("@SbDate", dtpDate.Value.ToString("yyyy-MM-dd"))

                Dim da As New SqlDataAdapter(cmd)
                Dim dt As New DataTable()
                da.Fill(dt)

                ' 데이터가 없을 경우 처리
                If dt.Rows.Count = 0 Then
                    MessageBox.Show("조회된 데이터가 없습니다.")
                    Cursor = Cursors.Default
                    Exit Sub
                End If

                ' 3. 데이터 가공 (VB6의 루프 로직 대체)
                ' 차액 컬럼 2개 추가 (Diff1: 현금+카드 차액, Diff2: 공급+부가 차액)
                dt.Columns.Add("Diff1", GetType(Decimal))
                dt.Columns.Add("Diff2", GetType(Decimal))

                ' 합계 변수 선언
                Dim sumTot As Long = 0
                Dim sumCash As Long = 0
                Dim sumCard As Long = 0
                Dim sumKo As Long = 0
                Dim sumVat As Long = 0
                Dim sumDiff1 As Long = 0
                Dim sumDiff2 As Long = 0

                ' 루프 돌며 차액 계산 및 합계 누적
                For Each row As DataRow In dt.Rows
                    ' DB Null 처리 및 값 가져오기
                    Dim tot As Long = If(IsDBNull(row("TotAmt")), 0, Convert.ToInt64(row("TotAmt")))
                    Dim cash As Long = If(IsDBNull(row("Cashamt")), 0, Convert.ToInt64(row("Cashamt")))
                    Dim card As Long = If(IsDBNull(row("Cardamt")), 0, Convert.ToInt64(row("Cardamt")))
                    Dim ko As Long = If(IsDBNull(row("Koamt")), 0, Convert.ToInt64(row("Koamt")))
                    Dim vat As Long = If(IsDBNull(row("VatAmt")), 0, Convert.ToInt64(row("VatAmt")))

                    ' 차액 계산 로직 (VB6: vTermAmt1, vTermAmt2)
                    Dim diff1 As Long = tot - (cash + card)
                    Dim diff2 As Long = tot - (ko + vat)

                    ' Row에 값 할당
                    row("Diff1") = diff1
                    row("Diff2") = diff2

                    ' 전체 합계 누적
                    sumTot += tot
                    sumCash += cash
                    sumCard += card
                    sumKo += ko
                    sumVat += vat
                    sumDiff1 += diff1
                    sumDiff2 += diff2
                Next

                ' 4. 합계 행(Row 1) 생성 및 맨 위에 삽입
                Dim totalRow As DataRow = dt.NewRow()
                totalRow("SSCodename") = "합계" ' 세외수입 명칭 컬럼에 표시
                totalRow("TotAmt") = sumTot
                totalRow("Cashamt") = sumCash
                totalRow("Cardamt") = sumCard
                totalRow("Koamt") = sumKo
                totalRow("VatAmt") = sumVat
                totalRow("Diff1") = sumDiff1
                totalRow("Diff2") = sumDiff2

                ' 맨 위(Index 0)에 삽입
                dt.Rows.InsertAt(totalRow, 0)

                ' 5. 그리드 바인딩 및 포맷 설정
                dgvIncome.DataSource = dt
                FormatGrid()

            End Using

        Catch ex As Exception
            MessageBox.Show("조회 중 오류 발생: " & ex.Message)
        Finally
            Cursor = Cursors.Default
        End Try

    End Sub
    ' [그리드 디자인 설정 함수]
    Private Sub FormatGrid()
        With dgvIncome
            ' 컬럼 헤더 이름 설정 (VB6 Spread 헤더와 매칭)
            .Columns("SSCodename").HeaderText = "세외수입"
            .Columns("Hkno").HeaderText = "세외수입코드"
            .Columns("TotAmt").HeaderText = "토탈"
            .Columns("Cashamt").HeaderText = "현금"
            .Columns("Cardamt").HeaderText = "카드"
            .Columns("Koamt").HeaderText = "공급가액"
            .Columns("VatAmt").HeaderText = "부가세"
            .Columns("Diff1").HeaderText = "토탈_현금카드_차액"
            .Columns("Diff2").HeaderText = "토탈_공급부가_차액"

            ' 숨길 컬럼들 (SQL에서 가져왔지만 화면엔 안 보이는 것들)
            .Columns("Gojino").Visible = False
            .Columns("Sbdate").Visible = False
            .Columns("SHangmok").Visible = False
            .Columns("KigwanCd").Visible = False
            .Columns("BusoCd").Visible = False
            .Columns("HkGbCd").Visible = False
            .Columns("SSCode").Visible = False

            ' 숫자 컬럼 포맷 (3자리 콤마) 및 우측 정렬
            Dim numCols() As String = {"TotAmt", "Cashamt", "Cardamt", "Koamt", "VatAmt", "Diff1", "Diff2"}

            For Each colName As String In numCols
                With .Columns(colName).DefaultCellStyle
                    .Format = "N0"
                    .Alignment = DataGridViewContentAlignment.MiddleRight
                End With
            Next

            ' 첫 번째 행(합계) 스타일 강조 (선택 사항)
            .Rows(0).DefaultCellStyle.BackColor = Color.LightYellow
            .Rows(0).DefaultCellStyle.Font = New Font(dgvIncome.Font, FontStyle.Bold)
            .Rows(0).Frozen = True ' 합계행 고정 (스크롤해도 보이게)

            ' 컬럼 폭 자동 조절
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        End With
    End Sub

    ' 체크 버튼 클릭 이벤트 (차액 자동 맞춤 로직이 들어갈 곳)
    Private Sub btnCheck_Click(sender As Object, e As EventArgs) Handles btnCheck.Click
        Dim inputAmount As Decimal

        If Decimal.TryParse(txtBankAmount.Text.Replace(",", ""), inputAmount) Then
            ' 여기에 차액 계산 로직 구현
            MessageBox.Show($"입력된 통장금액 {inputAmount:N0}원으로 차액 계산을 수행합니다.")

            ' 예시: 로직 구현 제안
            ' 1. 그리드의 '전산수입액' 총합 구하기
            ' 2. 입력된 '통장입금액'과 비교
            ' 3. 차액 발생 시 특정 행에 보정치를 넣거나 알림 표시
        Else
            MessageBox.Show("유효한 금액을 입력해주세요.")
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load

        ' 1. INI 파일 경로 설정 (실행 파일과 같은 폴더)
        Dim iniPath As String = System.Windows.Forms.Application.StartupPath & "\SWFMC.ini"

        ' 2. modINI를 사용하여 설정값 읽어오기 (기존 모듈 사용)
        Dim sHost As String = modINI.GetIni("DBServer", "Host", iniPath)
        Dim sDB As String = modINI.GetIni("DBServer", "DB", iniPath)
        Dim sEncUser As String = modINI.GetIni("DBServer", "User", iniPath) ' 암호화된 ID
        Dim sEncPass As String = modINI.GetIni("DBServer", "Pass", iniPath) ' 암호화된 PW

        ' 3. 읽어온 값이 비어있는지 확인
        If sHost = "" Or sDB = "" Then
            MessageBox.Show("SWFMC.ini 파일에서 DB 설정 정보를 읽을 수 없습니다." & vbCrLf & "경로: " & iniPath)
            Exit Sub
        End If

        Dim sAgentCode As String = modINI.GetIni("Pos Setup", "AgentCode", iniPath)

        ' 콤보박스 초기화
        cboBizCode.Items.Clear()

        If sAgentCode <> "" Then
            cboBizCode.Items.Add(sAgentCode)
            cboBizCode.SelectedIndex = 0
        Else
            cboBizCode.Items.Add("")
            cboBizCode.SelectedIndex = 0
        End If

        ' 4. 암호화된 ID/PW 복호화 (%115 -> s 변환)
        Dim sUser As String = DecodeAscii(sEncUser)
        Dim sPass As String = DecodeAscii(sEncPass)

        ' 5. 연결 문자열 생성 (modDBConn의 변수에 할당)
        ' .NET 8.0/MSSQL 접속을 위해 TrustServerCertificate=True 추가
        Dim connStr As String = $"Data Source={sHost};Initial Catalog={sDB};User ID={sUser};Password={sPass};TrustServerCertificate=True;Encrypt=False"

        ' 공통 모듈 변수에 주입
        modDBConn.ConnectionString = connStr

        ' 6. DB 연결 테스트
        Try
            Using conn As SqlConnection = modDBConn.GetConnection()
                If conn IsNot Nothing Then
                    ' 연결 성공 시 상태 표시줄이나 라벨에 표시 (필요 시 주석 해제)
                    'MessageBox.Show("DB 연결 성공!")
                    'Debug.WriteLine("DB 연결 성공: " & sHost)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("DB 연결 에러: " & ex.Message)
        End Try

    End Sub
    ' ---------------------------------------------------------
    ' [내부 함수] 아스키 코드 문자열(%115%97)을 실제 문자열로 변환
    ' ---------------------------------------------------------
    Private Function DecodeAscii(ByVal sInput As String) As String
        If String.IsNullOrEmpty(sInput) Then Return ""

        Dim result As New StringBuilder()

        ' "%" 문자를 기준으로 자릅니다.
        Dim parts() As String = sInput.Split("%"c)

        For Each part As String In parts
            ' 빈 문자열이 아니고 숫자인 경우만 문자로 변환
            If part <> "" AndAlso IsNumeric(part) Then
                Dim code As Integer = Integer.Parse(part)
                result.Append(ChrW(code))
            End If
        Next

        Return result.ToString()
    End Function
End Class