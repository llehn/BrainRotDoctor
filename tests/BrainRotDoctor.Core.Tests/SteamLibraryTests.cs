using BrainRotDoctor.Core.Apps;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class SteamLibraryTests
{
    [Fact]
    public void Reads_every_library_folder()
    {
        const string text = """
            "libraryfolders"
            {
            	"0"
            	{
            		"path"		"C:\\Steam"
            		"label"		""
            		"apps"
            		{
            			"22330"		"5931990491"
            		}
            	}
            	"1"
            	{
            		"path"		"D:\\Games\\SteamLibrary"
            	}
            }
            """;

        Assert.Equal(new[] { @"C:\Steam", @"D:\Games\SteamLibrary" }, SteamLibrary.ParseLibraryFolders(text));
    }

    [Fact]
    public void Reads_a_games_folder_and_name()
    {
        const string text = """
            "AppState"
            {
            	"appid"		"1091500"
            	"name"		"Cyberpunk 2077"
            	"installdir"		"Cyberpunk 2077"
            	"InstalledDepots"
            	{
            		"1091501"
            		{
            			"name"		"not this one"
            		}
            	}
            }
            """;

        Assert.Equal(new SteamGame("Cyberpunk 2077", "Cyberpunk 2077"), SteamLibrary.ParseAppManifest(text));
    }

    [Fact]
    public void A_name_with_quotes_is_unescaped_and_a_missing_name_falls_back_to_the_folder()
    {
        Assert.Equal("The \"Best\" Game", SteamLibrary.ParseAppManifest("\"name\" \"The \\\"Best\\\" Game\"\n\"installdir\" \"Best\"")!.Name);
        Assert.Equal("Valheim", SteamLibrary.ParseAppManifest("\"installdir\" \"Valheim\"")!.Name);
    }

    [Fact]
    public void A_manifest_without_a_folder_is_no_game()
    {
        Assert.Null(SteamLibrary.ParseAppManifest("\"name\" \"Half-installed\""));
        Assert.Null(SteamLibrary.ParseAppManifest(""));
    }
}
