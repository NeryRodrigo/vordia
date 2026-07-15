$connectionString = "Server=DESKTOP-8LS8I0A;Database=OIMA_DB;User Id=sa;Password=**nrcm**;TrustServerCertificate=True;"
$query = @"
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE RutaFrontend = 'produccion')
BEGIN
    INSERT INTO Modulos (NombreModulo, RubroAsociado, RutaFrontend, Icono)
    VALUES ('Produccion (MRP)', 'GENERAL', 'produccion', 'Factory');
END

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE RutaFrontend = 'contabilidad')
BEGIN
    INSERT INTO Modulos (NombreModulo, RubroAsociado, RutaFrontend, Icono)
    VALUES ('Contabilidad (FI)', 'GENERAL', 'contabilidad', 'BookOpen');
END

-- Asignar los nuevos módulos a los usuarios ADMIN (Rol = 'ADMIN') o a todos si querés
INSERT INTO UsuarioModulos (UsuarioId, ModuloId)
SELECT u.UsuarioId, m.ModuloId
FROM Usuarios u
CROSS JOIN Modulos m
WHERE m.RutaFrontend IN ('produccion', 'contabilidad')
AND NOT EXISTS (
    SELECT 1 FROM UsuarioModulos um 
    WHERE um.UsuarioId = u.UsuarioId AND um.ModuloId = m.ModuloId
);
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)

try {
    $connection.Open()
    $result = $command.ExecuteNonQuery()
    Write-Host "Modulos insertados exitosamente: $result"
} catch {
    Write-Host "Error: $_"
} finally {
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
