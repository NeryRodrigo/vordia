$connectionString = "Server=DESKTOP-8LS8I0A;Database=GESCO_DB;User Id=sa;Password=**nrcm**;TrustServerCertificate=True;"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    
    # Search in Personas (Inquilinos)
    $cmd = New-Object System.Data.SqlClient.SqlCommand("SELECT * FROM Personas WHERE RazonSocial LIKE '%Adri%' OR RazonSocial LIKE '%Becker%'", $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $table = New-Object System.Data.DataTable
    $adapter.Fill($table) | Out-Null
    Write-Host "--- Personas Found ---"
    foreach ($row in $table.Rows) {
        Write-Host "PersonaId: $($row['PersonaId']) | RazonSocial: $($row['RazonSocial'])"
    }

    # Search in Contratos
    $cmd.CommandText = "SELECT c.*, p.RazonSocial FROM ContratosInquilino c JOIN Personas p ON c.InquilinoId = p.PersonaId WHERE p.RazonSocial LIKE '%Adri%' OR p.RazonSocial LIKE '%Becker%'"
    $table2 = New-Object System.Data.DataTable
    $adapter.Fill($table2) | Out-Null
    Write-Host "`n--- Contratos Found ---"
    foreach ($row in $table2.Rows) {
        Write-Host "ContratoId: $($row['ContratoId']) | InquilinoId: $($row['InquilinoId']) ($($row['RazonSocial'])) | EstaActivo: $($row['EstaActivo'])"
    }

    # Search in Morosidades
    $cmd.CommandText = "SELECT m.*, p.RazonSocial FROM MorosidadesInquilino m JOIN Personas p ON m.InquilinoId = p.PersonaId WHERE p.RazonSocial LIKE '%Adri%' OR p.RazonSocial LIKE '%Becker%'"
    $table3 = New-Object System.Data.DataTable
    $adapter.Fill($table3) | Out-Null
    Write-Host "`n--- Morosidades Found ---"
    foreach ($row in $table3.Rows) {
        Write-Host "MorosidadId: $($row['MorosidadId']) | InquilinoId: $($row['InquilinoId']) ($($row['RazonSocial'])) | MontoTotal: $($row['MontoTotal']) | Estado: $($row['Estado'])"
    }

} catch {
    Write-Host "Error: $_"
} finally {
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
