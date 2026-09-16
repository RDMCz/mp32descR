PROJNAME="mp32descR"
PUBLISHDIR="./PUBLISHED"

publish:
	dotnet publish ./$(PROJNAME)/$(PROJNAME).csproj -r linux-x64 -c Release -o $(PUBLISHDIR)

clean:
	rm -rf $(PUBLISHDIR)