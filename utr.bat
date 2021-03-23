@echo OFF
SET VERSION=5930170
SET PREVIEW=1
SET UTR_STANDALONE_URL=https://artifactory.prd.it.unity3d.com/artifactory/unity-tools-local/utr-standalone/
SET UNZIP_TOOL_URL=https://artifactory.prd.it.unity3d.com/artifactory/unity-tools-local/Windows/unzip.exe

if "%USERNAME%" == "bokken" (
	SET UTR_STANDALONE_URL=http://artifactory-slo.bf.unity3d.com/artifactory/unity-tools-local/utr-standalone/
	SET UNZIP_TOOL_URL=http://artifactory-slo.bf.unity3d.com/artifactory/unity-tools-local/Windows/unzip.exe
)

if "%PREVIEW%" == "1" (
	SET UTR_STANDALONE_URL=%UTR_STANDALONE_URL%preview/
)

if defined UTR_VERSION if NOT "%UTR_VERSION%" == "current" (
	echo Environment variable UTR_VERSION is set to '%UTR_VERSION%'. Using %UTR_VERSION% instead of current version '%VERSION%'
	SET VERSION=%UTR_VERSION%
)

SET UTR_STANDALONE_URL=%UTR_STANDALONE_URL%utr-standalone-win-%VERSION%.zip

SET DOWNLOAD_DIR=%cd%\.download
mkdir "%DOWNLOAD_DIR%"

SET DEST_FILE=%DOWNLOAD_DIR%\utr.%VERSION%.win.zip
if NOT EXIST %DEST_FILE% (
	echo "Downloading standalone utr from %UTR_STANDALONE_URL%"
	curl %UTR_STANDALONE_URL% --output "%DEST_FILE%"
)

SET TARGETDIR=%cd%\.bin\utr.%VERSION%
IF NOT EXIST "%TARGETDIR%" (
	MKDIR "%TARGETDIR%"
	where /q 7z
	IF ERRORLEVEL 1 (
		IF NOT EXIST  "%DOWNLOAD_DIR%\unzip.exe" (
			echo "7zip not found. It is recomeneded to have it installed. Downloading %UNZIP_TOOL_URL% to %DOWNLOAD_DIR%"
			curl -s %UNZIP_TOOL_URL% --output "%DOWNLOAD_DIR%\unzip.exe"
		)
		echo "Unzipping %DEST_FILE% --> %TARGETDIR%
		"%DOWNLOAD_DIR%\unzip.exe" -q "%DEST_FILE%" -d "%TARGETDIR%"
	) ELSE (
		echo "Unzipping %DEST_FILE% --> %TARGETDIR%
		7z x "%DEST_FILE%" -o"%TARGETDIR%" > nul
	)
)

"%TARGETDIR%\UnifiedTestRunner" %*
