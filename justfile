default:
	just --list

run:
	dotnet run --project src/TestApplication/TestApplication.csproj

test:
    dotnet test --logger:"console;verbosity=normal"

docs:
    docfx docfx.json --serve --open-browser

clear-docs:
    rm -rf docs/_site
    rm -rf docs/api
