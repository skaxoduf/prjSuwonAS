Imports System.Configuration
Imports System.IO
Imports Microsoft.Data.SqlClient   '.net8.0에서는 이 네임스페이스 사용이 권장됨

Module modDBConn
    ' 프로그램 전체에서 사용할 DB 연결 정보 문자열
    Public ConnectionString As String
    Public Function GetConnection() As SqlConnection
        If String.IsNullOrEmpty(ConnectionString) Then
            MessageBox.Show("데이터베이스 연결 정보가 설정되지 않았습니다.", "설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        End If
        Try
            Dim conn As New SqlConnection(ConnectionString)
            conn.Open()
            Return conn
        Catch ex As SqlException
            MessageBox.Show("데이터베이스 연결 실패: " & vbCrLf & ex.Message, "DB 오류", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        Catch ex As Exception
            MessageBox.Show("알 수 없는 오류가 발생했습니다." & vbCrLf & ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        End Try
    End Function
    Public Sub CloseConnection(ByVal conn As SqlConnection)
        If conn IsNot Nothing AndAlso conn.State = ConnectionState.Open Then
            conn.Close()
            conn.Dispose()
        End If
    End Sub

End Module
