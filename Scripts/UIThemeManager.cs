using Godot;

public static class UIThemeManager {
    public static void ApplyPremiumTheme(Node root) {
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.12f, 0.14f, 0.2f, 0.9f); 
        panelStyle.CornerRadiusTopLeft = 12;
        panelStyle.CornerRadiusTopRight = 12;
        panelStyle.CornerRadiusBottomLeft = 12;
        panelStyle.CornerRadiusBottomRight = 12;
        panelStyle.BorderWidthBottom = 4;
        panelStyle.BorderColor = new Color(0.08f, 0.1f, 0.15f, 0.9f);
        panelStyle.BorderWidthTop = 1;
        panelStyle.BorderColor = new Color(0.2f, 0.25f, 0.35f, 0.5f);
        panelStyle.ShadowColor = new Color(0f, 0f, 0f, 0.6f);
        panelStyle.ShadowSize = 15;
        
        var btnNormal = new StyleBoxFlat();
        btnNormal.BgColor = new Color(0.2f, 0.25f, 0.35f, 0.8f);
        btnNormal.CornerRadiusTopLeft = 8;
        btnNormal.CornerRadiusTopRight = 8;
        btnNormal.CornerRadiusBottomLeft = 8;
        btnNormal.CornerRadiusBottomRight = 8;
        btnNormal.BorderWidthBottom = 2;
        btnNormal.BorderColor = new Color(0.1f, 0.15f, 0.25f, 0.9f);
        
        var btnHover = (StyleBoxFlat)btnNormal.Duplicate();
        btnHover.BgColor = new Color(0.35f, 0.5f, 0.8f, 1f); 
        
        Theme theme = new Theme();
        theme.SetStylebox("panel", "Panel", panelStyle);
        theme.SetStylebox("panel", "PanelContainer", panelStyle);
        theme.SetStylebox("normal", "Button", btnNormal);
        theme.SetStylebox("hover", "Button", btnHover);
        theme.SetStylebox("pressed", "Button", btnNormal);
        theme.SetColor("font_color", "Label", new Color(0.9f, 0.95f, 1f)); 
        
        ApplyToAll(root, theme);
    }
    
    private static void ApplyToAll(Node n, Theme theme) {
        if (n is Control c) {
            c.Theme = theme;
        }
        foreach(Node child in n.GetChildren()) {
            ApplyToAll(child, theme);
        }
    }
}
