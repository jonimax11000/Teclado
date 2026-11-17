using NUnit.Framework;
using UnityEditor.SceneManagement;

public class GlyphAuditTests
{
    [TearDown] public void TearDown() => EditorSceneManager.OpenScene("Assets/Scenes/Pruebas VR.unity");

    [Test] public void TodasLasDistribuciones_SinGlifosFaltantes()
    {
        var missing = GlyphAudit.Missing();
        Assert.IsEmpty(missing, string.Join("\n", missing));
    }
}
