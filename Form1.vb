Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient ' .NET 8.0 환경 

Public Class Form1


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load

        Dim iniPath As String = System.Windows.Forms.Application.StartupPath & "\SWFMC.ini"

        Dim sHost As String = modINI.GetIni("DBServer", "Host", iniPath)
        Dim sDB As String = modINI.GetIni("DBServer", "DB", iniPath)
        Dim sEncUser As String = modINI.GetIni("DBServer", "User", iniPath) ' 암호화된 ID
        Dim sEncPass As String = modINI.GetIni("DBServer", "Pass", iniPath) ' 암호화된 PW

        If sHost = "" Or sDB = "" Then
            MessageBox.Show("SWFMC.ini 파일에서 DB 설정 정보를 읽을 수 없습니다." & vbCrLf & "경로: " & iniPath)
            Exit Sub
        End If

        Dim sAgentCode As String = modINI.GetIni("Pos Setup", "AgentCode", iniPath)

        cboBizCode.Items.Clear()

        If sAgentCode = "17110003" Then   '' 종합운동장만 사용
            cboBizCode.Items.Add(sAgentCode)
            cboBizCode.SelectedIndex = 0
        Else
            cboBizCode.Items.Add("")
            cboBizCode.SelectedIndex = 0
        End If

        ' 아이디 비번 복호화 (%115 -> s)
        Dim sUser As String = DecodeAscii(sEncUser)
        Dim sPass As String = DecodeAscii(sEncPass)

        ' 연결 문자열 생성 (modDBConn의 변수에 할당)
        Dim connStr As String = $"Data Source={sHost};Initial Catalog={sDB};User ID={sUser};Password={sPass};TrustServerCertificate=True;Encrypt=False"

        ' 공통 모듈 변수에 주입
        modDBConn.ConnectionString = connStr

        ' 6. DB 연결 테스트
        Try
            Using conn As SqlConnection = modDBConn.GetConnection()
                If conn IsNot Nothing Then
                    'Debug.WriteLine("DB 연결 성공: " & sHost)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("DB 연결 에러: " & ex.Message)
        End Try

    End Sub
    ' 아스키 코드 문자열(%115%97)을 실제 문자열로 변환
    Private Function DecodeAscii(ByVal sInput As String) As String
        If String.IsNullOrEmpty(sInput) Then Return ""

        Dim result As New StringBuilder()

        ' "%" 문자를 기준으로 자른다.
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


    ' 조회 버튼 클릭 이벤트
    Private Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click

        If cboBizCode.Text = "" Then
            MessageBox.Show("업장코드를 선택하세요.")
            Return
        End If

        Cursor = Cursors.WaitCursor
        dgvIncome.DataSource = Nothing ' 그리드 초기화

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
                ' 파라미터 바인딩 
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


                ' 데이터 가공 
                If Not dt.Columns.Contains("No") Then dt.Columns.Add("No", GetType(String))
                If Not dt.Columns.Contains("Diff1") Then dt.Columns.Add("Diff1", GetType(Decimal))
                If Not dt.Columns.Contains("Diff2") Then dt.Columns.Add("Diff2", GetType(Decimal))


                ' 합계 변수 선언
                Dim sumTot As Long = 0
                Dim sumCash As Long = 0
                Dim sumCard As Long = 0
                Dim sumKo As Long = 0
                Dim sumVat As Long = 0
                Dim sumDiff1 As Long = 0
                Dim sumDiff2 As Long = 0

                ' 루프 돌며 차액 계산 및 합계 누적
                Dim idx As Integer = 1
                For Each row As DataRow In dt.Rows

                    row("No") = idx.ToString()
                    idx += 1

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
    Private Sub FormatGrid()
        With dgvIncome
            .ReadOnly = True
            .AllowUserToResizeRows = False ' 행 높이 조절 불가
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing ' 헤더 높이 조절 불가
            .AllowUserToAddRows = False  ' 맨 밑에 빈 행 추가 안되게

            ' 1. No (순번) 컬럼 생성
            If .Columns("No") Is Nothing Then
                Dim colNo As New DataGridViewTextBoxColumn()
                colNo.Name = "No"
                .Columns.Insert(0, colNo)
            End If
            .Columns("No").DataPropertyName = "No"
            .Columns("No").HeaderText = "No"
            .Columns("No").Width = 50
            .Columns("No").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("No").DisplayIndex = 0


            ' 2. Diff1 (차액1) 컬럼 생성
            If .Columns("Diff1") Is Nothing Then
                Dim colDiff1 As New DataGridViewTextBoxColumn()
                colDiff1.Name = "Diff1"
                colDiff1.HeaderText = "토탈_현금카드_차액"
                colDiff1.DataPropertyName = "Diff1"
                .Columns.Add(colDiff1)
            End If

            ' 3. Diff2 (차액2) 컬럼 생성
            If .Columns("Diff2") Is Nothing Then
                Dim colDiff2 As New DataGridViewTextBoxColumn()
                colDiff2.Name = "Diff2"
                colDiff2.HeaderText = "토탈_공급부가_차액"
                colDiff2.DataPropertyName = "Diff2"
                .Columns.Add(colDiff2)
            End If




            ' 컬럼 제목 줄바꿈 방지 (강제로 한 줄로 표시)
            .ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False
            .ColumnHeadersHeight = 30
            .EnableHeadersVisualStyles = False
            .ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke
            .ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

            .RowHeadersVisible = False ' 행 헤더(맨 왼쪽 회색 바) 숨김 -> No 컬럼으로 대체

            ' 0. No
            If .Columns("No") IsNot Nothing Then
                .Columns("No").HeaderText = "No"
                .Columns("No").DisplayIndex = 0
                .Columns("No").Width = 50
                .Columns("No").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If

            ' 1. 세외수입
            If .Columns("SSCodename") IsNot Nothing Then
                .Columns("SSCodename").HeaderText = "세외수입"
                .Columns("SSCodename").DisplayIndex = 1
                .Columns("SSCodename").Width = 150
                .Columns("SSCodename").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If

            ' 2. 토탈 (여기 DisplayIndex=2 를 꼭 넣어줘야 합니다)
            If .Columns("TotAmt") IsNot Nothing Then
                .Columns("TotAmt").HeaderText = "토탈"
                .Columns("TotAmt").DisplayIndex = 2
            End If

            ' 3. 현금 (DisplayIndex=3)
            If .Columns("Cashamt") IsNot Nothing Then
                .Columns("Cashamt").HeaderText = "현금"
                .Columns("Cashamt").DisplayIndex = 3
            End If

            ' 4. 카드 (DisplayIndex=4)
            If .Columns("Cardamt") IsNot Nothing Then
                .Columns("Cardamt").HeaderText = "카드"
                .Columns("Cardamt").DisplayIndex = 4
            End If

            ' 5. 공급가액
            If .Columns("Koamt") IsNot Nothing Then
                .Columns("Koamt").HeaderText = "공급가액"
                .Columns("Koamt").DisplayIndex = 5
            End If

            ' 6. 부가세
            If .Columns("VatAmt") IsNot Nothing Then
                .Columns("VatAmt").HeaderText = "부가세"
                .Columns("VatAmt").DisplayIndex = 6
            End If

            ' 7. 차액 컬럼들
            If .Columns("Diff1") IsNot Nothing Then
                .Columns("Diff1").HeaderText = "토탈_현금카드_차액"
                .Columns("Diff1").DisplayIndex = 7
            End If

            If .Columns("Diff2") IsNot Nothing Then
                .Columns("Diff2").HeaderText = "토탈_공급부가_차액"
                .Columns("Diff2").DisplayIndex = 8
            End If

            ' [2] 숨김 컬럼 처리 (존재 여부 체크 후 숨김)
            Dim hideCols() As String = {"Hkno", "Gojino", "Sbdate", "SHangmok", "KigwanCd", "BusoCd", "HkGbCd", "SSCode"}
            For Each cName As String In hideCols
                If .Columns(cName) IsNot Nothing Then .Columns(cName).Visible = False
            Next

            ' 3. 숫자 포맷 및 정렬
            Dim numCols() As String = {"TotAmt", "Cashamt", "Cardamt", "Koamt", "VatAmt", "Diff1", "Diff2"}
            For Each colName As String In numCols
                If .Columns(colName) IsNot Nothing Then
                    With .Columns(colName).DefaultCellStyle
                        .Format = "N0"
                        .Alignment = DataGridViewContentAlignment.MiddleRight
                    End With
                End If
            Next

            If .Rows.Count > 0 Then
                .Rows(0).DefaultCellStyle.BackColor = Color.White
                .Rows(0).DefaultCellStyle.Font = New Font(dgvIncome.Font, FontStyle.Bold)
                .Rows(0).Frozen = True
            End If

            ' 컬럼 폭 자동 조절
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        End With
    End Sub

    ' 체크 버튼 클릭 이벤트
    Private Sub btnCheck_Click(sender As Object, e As EventArgs) Handles btnCheck.Click

        If dgvIncome.Rows.Count = 0 Then Exit Sub

        Dim inputAmt As Long
        If Not Long.TryParse(txtBankAmount.Text.Replace(",", ""), inputAmt) Then
            MessageBox.Show("체크할 통장입금액을 입력하세요!", "입력 오류")
            txtBankAmount.Focus()
            Exit Sub
        End If

        Dim sysCardAmt As Long = 0
        If Not IsDBNull(dgvIncome.Rows(0).Cells("Cardamt").Value) Then
            sysCardAmt = Convert.ToInt64(dgvIncome.Rows(0).Cells("Cardamt").Value)
        End If


        Dim diffAmt As Long = inputAmt - sysCardAmt
        If diffAmt <= 0 Then
            MessageBox.Show("차액 금액이 0원 이하입니다. (차액 없음)", "알림")
            Exit Sub
        End If
        If MessageBox.Show($"차액 금액이 {diffAmt:N0} 원 발생합니다." & vbCrLf &
                           "차액만큼 DB에 보정 데이터를 생성하시겠습니까?",
                           "차액 보정", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Exit Sub
        End If

        Cursor = Cursors.WaitCursor
        Dim isSuccess As Boolean = False

        ' ---------------------------------------------------------
        ' [DB 트랜잭션 시작]
        ' ---------------------------------------------------------
        Using conn As SqlConnection = modDBConn.GetConnection()
            If conn Is Nothing Then Exit Sub

            Dim trans As SqlTransaction = conn.BeginTransaction() ' 트랜잭션 시작
            Try
                Dim sAgentCode As String = cboBizCode.Text.Trim()
                Dim sPosNo As String = "01" ' 포스번호 고정값
                Dim nowTime As DateTime = DateTime.Now

                ' -----------------------------------------------------
                ' 5. 영수증 번호(TrsNo) 생성 로직
                ' 업장(8) + 시분(4) + 포스(2) = 14자리 Prefix
                ' -----------------------------------------------------
                Dim sTimePart As String = nowTime.ToString("HHmm")

                Dim makeTrsNo As String = sAgentCode & sTimePart & sPosNo
                Dim sTrsNo As String = ""

                ' MAX 번호 조회
                Dim sqlMax As String = "SELECT MAX(TrsNo) FROM T_TRSINOUT WHERE LEFT(TrsNo, 14) = @Prefix AND AgentCode = @AgentCode"

                Using cmdMax As New SqlCommand(sqlMax, conn, trans)
                    cmdMax.Parameters.AddWithValue("@Prefix", makeTrsNo)
                    cmdMax.Parameters.AddWithValue("@AgentCode", sAgentCode)

                    Dim result As Object = cmdMax.ExecuteScalar()

                    If result IsNot Nothing AndAlso Not IsDBNull(result) AndAlso result.ToString().Trim() <> "" Then
                        Dim maxTrsNo As String = result.ToString().Trim()
                        If maxTrsNo.Length >= 20 Then
                            Dim seq As Integer = Integer.Parse(maxTrsNo.Substring(14, 6))
                            sTrsNo = makeTrsNo & (seq + 1).ToString("D6")
                        Else
                            sTrsNo = makeTrsNo & "000001"
                        End If
                    Else
                        ' 데이터가 없으면 000001
                        sTrsNo = makeTrsNo & "000001"
                    End If
                End Using

                ' -----------------------------------------------------
                ' 6. T_TRSINOUT 테이블 Insert
                ' -----------------------------------------------------
                Dim sqlInsert1 As String = "
                    INSERT INTO T_TRSINOUT (
                        AgentCode, Trsno, Trsseq, Trsdate, Trstime, Trsweek, Mbno,
                        Jsdate, Sbdate, Spdate, Skdate, Tgcode, Rbcode, Tjcode,
                        Trcode, AmtGb, Cashamt, Cardamt, Totamt, SkRegCnt, SkJanCnt, SkChkGb,
                        DayInCnt, SuCnt, DanGa, DcCode, DcGb, DcRate, DcAmt,
                        Fbsamt, Ksamt, Lbsamt, Koamt, Vatamt, Gojino, VAccNo,
                        Hkno, Skbno, Startdate, Enddate, Lcode, Lcdname, LcMonCnt, Lno,
                        SpNo, SpGbname, Spname, SpUseTime, DCMGubun, DCMGbname, ParkTime,
                        CarNo, CarNo2, WebAccYn, LinkTrsno, LinkTrsseq, PosStr, Posno, Sno, Sname,
                        Bigo, ChkData, Isdel
                    ) VALUES (
                        @AgentCode, @TrsNo, '1', @TrsDate, @TrsTime, @TrsWeek, '',
                        @SbDate, @SbDate, '', '', '1', '7', '1',
                        '1', '2', '0', @DiffAmt, @DiffAmt, '0', '0', '',
                        '0', '1', '0', '', '', '0', '0', 
                        @DiffAmt, '0', @DiffAmt, '0', '0', '', '', 
                        '0010', '', '', '', '', '', '0', '', 
                        '17120001', '주차', '일주차-남문', '0', '', '', '0', 
                        '', '', 'N', '', '0', '00000000000000000000', @PosNo, '', '대양수퍼관리자',
                        '', '정상', 'N'
                    )"

                Using cmd1 As New SqlCommand(sqlInsert1, conn, trans)
                    cmd1.Parameters.AddWithValue("@AgentCode", sAgentCode)
                    cmd1.Parameters.AddWithValue("@TrsNo", sTrsNo)
                    cmd1.Parameters.AddWithValue("@TrsDate", nowTime.ToString("yyyy-MM-dd"))
                    cmd1.Parameters.AddWithValue("@TrsTime", nowTime.ToString("HH:mm:ss"))
                    cmd1.Parameters.AddWithValue("@TrsWeek", GetDayName(nowTime))
                    cmd1.Parameters.AddWithValue("@SbDate", dtpDate.Value.ToString("yyyy-MM-dd"))
                    cmd1.Parameters.AddWithValue("@DiffAmt", diffAmt)
                    cmd1.Parameters.AddWithValue("@PosNo", sPosNo)
                    cmd1.ExecuteNonQuery()
                End Using

                ' -----------------------------------------------------
                ' 7. T_TRSINOUTGB 테이블 Insert (추가된 부분)
                ' -----------------------------------------------------
                Dim sqlInsertGB As String = "
                    INSERT INTO T_TRSINOUTGB (
                        AgentCode, Trsno, Trsdate, Trstime, Trsweek, Jsdate,
                        Tgcode, Cashamt, Cardamt, Totamt, DcAmt, Fbsamt, Ksamt, Lbsamt,
                        Koamt, Vatamt, Sbdate, Spdate, Skdate, Mbno, Posno, Sno, Sname, ChkData, Isdel
                    ) VALUES (
                        @AgentCode, @TrsNo, @TrsDate, @TrsTime, @TrsWeek, @SbDate,
                        '1', '0', @DiffAmt, @DiffAmt, '0', @DiffAmt, '0', @DiffAmt,
                        '0', '0', @SbDate, '', '', '', @PosNo, '', '대양수퍼관리자', '정상', 'N'
                    )"

                Using cmdGB As New SqlCommand(sqlInsertGB, conn, trans)
                    cmdGB.Parameters.AddWithValue("@AgentCode", sAgentCode)
                    cmdGB.Parameters.AddWithValue("@TrsNo", sTrsNo)
                    cmdGB.Parameters.AddWithValue("@TrsDate", nowTime.ToString("yyyy-MM-dd"))
                    cmdGB.Parameters.AddWithValue("@TrsTime", nowTime.ToString("HH:mm:ss"))
                    cmdGB.Parameters.AddWithValue("@TrsWeek", GetDayName(nowTime))
                    cmdGB.Parameters.AddWithValue("@SbDate", dtpDate.Value.ToString("yyyy-MM-dd"))
                    cmdGB.Parameters.AddWithValue("@DiffAmt", diffAmt)
                    cmdGB.Parameters.AddWithValue("@PosNo", sPosNo)
                    cmdGB.ExecuteNonQuery()
                End Using

                ' -----------------------------------------------------
                ' 8. T_TRSCARD 테이블 Insert
                ' -----------------------------------------------------
                Dim sqlInsertCard As String = "
                    INSERT INTO T_TRSCARD (
                        AgentCode, TrsNo, GJong, GDate, GTime, GWeek, GDateTime,
                        Pay, CardNo, CCode, CardSa, CardOKNo, CardOkDate, Halbu,
                        Junnum, GaMeangNo, MCode, Maeipsa, Notice, VanNumber,
                        PosNo, Sno, Sname, Isdel
                    ) VALUES (
                        @AgentCode, @TrsNo, '신용승인', @TrsDate, @TrsTime, @TrsWeek, @GDateTime,
                        @DiffAmt, '', '', '', '', @TrsDate, '0',
                        '', '', '', '', '', '',
                        @PosNo, '', '대양수퍼관리자', 'N'
                    )"

                Using cmdCard As New SqlCommand(sqlInsertCard, conn, trans)
                    cmdCard.Parameters.AddWithValue("@AgentCode", sAgentCode)
                    cmdCard.Parameters.AddWithValue("@TrsNo", sTrsNo)
                    cmdCard.Parameters.AddWithValue("@TrsDate", nowTime.ToString("yyyy-MM-dd"))
                    cmdCard.Parameters.AddWithValue("@TrsTime", nowTime.ToString("HH:mm:ss"))
                    cmdCard.Parameters.AddWithValue("@TrsWeek", GetDayName(nowTime))
                    cmdCard.Parameters.AddWithValue("@GDateTime", nowTime.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmdCard.Parameters.AddWithValue("@DiffAmt", diffAmt)
                    cmdCard.Parameters.AddWithValue("@PosNo", sPosNo)
                    cmdCard.ExecuteNonQuery()
                End Using

                ' -----------------------------------------------------
                ' 9. 트랜잭션 커밋
                ' -----------------------------------------------------
                trans.Commit()
                isSuccess = True ' 성공 플래그 ON
                MessageBox.Show($"보정이 완료되었습니다.{vbCrLf}", "성공")

            Catch ex As Exception
                trans.Rollback() ' 하나라도 에러나면 전체 취소
                Cursor = Cursors.Default
                MessageBox.Show("보정 처리 중 오류가 발생하여 취소되었습니다." & vbCrLf & ex.Message, "오류")
            End Try
        End Using

        If isSuccess Then
            Me.BeginInvoke(Sub() btnSearch.PerformClick())
        End If

        Cursor = Cursors.Default

    End Sub
    '  요일 구하기
    Private Function GetDayName(ByVal dt As DateTime) As String
        Dim culture As New System.Globalization.CultureInfo("ko-KR")
        Return culture.DateTimeFormat.GetDayName(dt.DayOfWeek).Substring(0, 1) ' "월요일" -> "월"
    End Function

End Class