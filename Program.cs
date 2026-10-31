using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace WolfMatrixLTB;

internal record LtbInfo(string Path, long Length, uint Signature, uint Version,
    uint AnimationCount, uint BoneCount, uint PieceCount, long[] CryOffsets);

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InspectorForm());
    }
}

internal sealed class InspectorForm : Form
{
    private string? pedro;
    private string? leviathan;
    private readonly TextBox report = new()
    {
        Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 10), WordWrap = false
    };

    public InspectorForm()
    {
        Text = "WolfMatrix LTB 25 - T_CRY Kontrol (Salt Okunur)";
        Size = new Size(850, 580);
        StartPosition = FormStartPosition.CenterScreen;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
        var choosePedro = new Button { Text = "Pedro LTB sec", Width = 220, Height = 40 };
        var chooseLevi = new Button { Text = "Leviathan LTB sec", Width = 220, Height = 40 };
        var compare = new Button { Text = "Karsilastir", Width = 220, Height = 40 };
        choosePedro.Click += (_, _) => { pedro = Choose("wt_se_firewolf_ch.ltb dosyasini sec"); if (pedro != null) report.Text = "Pedro: " + pedro; };
        chooseLevi.Click += (_, _) => { leviathan = Choose("wt_wolfrush_leviathan_ch.ltb dosyasini sec"); if (leviathan != null) report.Text += "\r\nLeviathan: " + leviathan; };
        compare.Click += (_, _) => Compare();
        panel.Controls.AddRange(new Control[] { choosePedro, chooseLevi, compare });
        Controls.Add(report);
        Controls.Add(panel);
    }

    private static string? Choose(string title)
    {
        using var dialog = new OpenFileDialog { Title = title, Filter = "LTB dosyasi (*.ltb)|*.ltb" };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private static LtbInfo Inspect(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 48) throw new InvalidDataException("LTB basligi eksik.");
        uint U32(int offset) => BitConverter.ToUInt32(data, offset);
        var needle = Encoding.ASCII.GetBytes("T_CRY");
        var positions = new System.Collections.Generic.List<long>();
        for (int i = 0; i <= data.Length - needle.Length; i++)
            if (data[i] == needle[0] && data.AsSpan(i, needle.Length).SequenceEqual(needle))
                positions.Add(i);
        return new LtbInfo(path, data.Length, U32(0), U32(20), U32(28), U32(32), U32(36), positions.ToArray());
    }

    private void Compare()
    {
        if (pedro == null || leviathan == null) { MessageBox.Show("Once iki LTB dosyasini sec."); return; }
        try
        {
            var p = Inspect(pedro);
            var l = Inspect(leviathan);
            report.Text = $"PEDRO\r\n Surum: {p.Version}  Kemik: {p.BoneCount}  Animasyon: {p.AnimationCount}  Parca: {p.PieceCount}\r\n" +
                $" T_CRY metin konumlari: {string.Join(", ", p.CryOffsets)}\r\n" +
                $"LEVIATHAN\r\n Surum: {l.Version}  Kemik: {l.BoneCount}  Animasyon: {l.AnimationCount}  Parca: {l.PieceCount}\r\n" +
                $" T_CRY metin konumlari: {string.Join(", ", l.CryOffsets)}\r\n\r\n" +
                $"Surum eslesmesi: {p.Version == l.Version}\r\nKemik sayisi eslesmesi: {p.BoneCount == l.BoneCount}\r\n\r\n" +
                "UYARI: T_CRY metin konumu animasyon veri blogunun siniri degildir.\r\n" +
                "Bu program oyun dosyalarini DEGISTIRMEZ.";
            if (p.Signature != 0x00090001 || l.Signature != 0x00090001)
                report.Text += "\r\nUYARI: Beklenmeyen PC LTB imzasi.";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "LTB okuma hatasi"); }
    }
}
