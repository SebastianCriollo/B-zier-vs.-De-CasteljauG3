$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vs = & $vswhere -latest -products Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -property installationPath
if (-not $vs) { throw 'Se requiere Visual Studio con desarrollo de escritorio .NET.' }
$csc = Join-Path $vs 'MSBuild/Current/Bin/Roslyn/csc.exe'
$refs = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8'
$out = Join-Path $repo 'src/BezierCore/bin/Validacion'
New-Item -ItemType Directory -Force $out | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src/BezierCore') -Filter '*.cs' | ForEach-Object FullName)
& $csc /nologo /langversion:7.3 /optimize+ /target:exe /nostdlib+ "/reference:$refs/mscorlib.dll" "/reference:$refs/System.dll" "/reference:$refs/System.Core.dll" "/out:$out/VerificarCore.exe" @sources (Join-Path $PSScriptRoot 'VerificarCore.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de la validación.' }
& (Join-Path $out 'VerificarCore.exe')
if ($LASTEXITCODE -ne 0) { throw 'Falló una comprobación matemática.' }
