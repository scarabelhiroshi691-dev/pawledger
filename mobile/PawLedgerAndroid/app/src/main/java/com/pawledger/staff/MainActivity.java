package com.pawledger.staff;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.Typeface;
import android.net.Uri;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.CookieManager;
import android.webkit.SslErrorHandler;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

public final class MainActivity extends Activity {
    private static final String PREFS = "pawledger_mobile";
    private static final String SERVER_URL = "server_url";
    private static final int NAVY = Color.rgb(8, 43, 87);
    private static final int TEAL = Color.rgb(0, 152, 142);
    private static final int INK = Color.rgb(13, 38, 70);
    private static final int MUTED = Color.rgb(89, 108, 132);
    private static final int BORDER = Color.rgb(213, 224, 236);

    private WebView webView;
    private ProgressBar progress;
    private String serverUrl;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(NAVY);
        serverUrl = getSharedPreferences(PREFS, MODE_PRIVATE).getString(SERVER_URL, "");
        if (serverUrl.isBlank()) showSetup(); else showBrowser();
    }

    private void showSetup() {
        LinearLayout root = vertical(24);
        root.setPadding(dp(28), dp(44), dp(28), dp(24));
        root.setGravity(Gravity.CENTER_HORIZONTAL);

        TextView mark = text("爪", 30, Color.WHITE, Typeface.BOLD);
        mark.setGravity(Gravity.CENTER);
        mark.setBackgroundColor(NAVY);
        root.addView(mark, new LinearLayout.LayoutParams(dp(72), dp(72)));

        TextView title = text("连接店内电脑", 26, INK, Typeface.BOLD);
        LinearLayout.LayoutParams titleLp = wrap(); titleLp.topMargin = dp(30);
        root.addView(title, titleLp);

        TextView intro = text("手机和电脑连接同一个门店 Wi‑Fi，然后输入电脑端“管理”页显示的地址。", 15, MUTED, Typeface.NORMAL);
        intro.setGravity(Gravity.CENTER); intro.setLineSpacing(0, 1.25f);
        LinearLayout.LayoutParams introLp = matchWrap(); introLp.topMargin = dp(12);
        root.addView(intro, introLp);

        EditText address = new EditText(this);
        address.setSingleLine(true);
        address.setText(serverUrl);
        address.setHint("例如：192.168.1.23");
        address.setTextSize(17);
        address.setPadding(dp(16), 0, dp(16), 0);
        LinearLayout.LayoutParams inputLp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(58));
        inputLp.topMargin = dp(30);
        root.addView(address, inputLp);

        Button connect = new Button(this);
        connect.setText("连接爪账");
        connect.setTextColor(Color.WHITE);
        connect.setTextSize(17);
        connect.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        connect.setBackgroundColor(TEAL);
        LinearLayout.LayoutParams buttonLp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(56));
        buttonLp.topMargin = dp(18);
        root.addView(connect, buttonLp);

        TextView note = text("不会上传会员或资金数据。电脑关闭后，手机端会暂时无法使用。", 13, MUTED, Typeface.NORMAL);
        note.setGravity(Gravity.CENTER);
        LinearLayout.LayoutParams noteLp = matchWrap(); noteLp.topMargin = dp(20);
        root.addView(note, noteLp);

        connect.setOnClickListener(v -> {
            try {
                serverUrl = ServerAddress.normalize(address.getText().toString());
                getSharedPreferences(PREFS, MODE_PRIVATE).edit().putString(SERVER_URL, serverUrl).apply();
                showBrowser();
            } catch (IllegalArgumentException ex) {
                address.setError(ex.getMessage());
            }
        });
        setContentView(root);
    }

    @SuppressLint("SetJavaScriptEnabled") // PawLedger's Razor UI uses JavaScript; navigation is restricted to the configured host.
    private void showBrowser() {
        LinearLayout root = vertical(0);
        LinearLayout bar = new LinearLayout(this);
        bar.setOrientation(LinearLayout.HORIZONTAL);
        bar.setGravity(Gravity.CENTER_VERTICAL);
        bar.setPadding(dp(16), 0, dp(8), 0);
        bar.setBackgroundColor(NAVY);

        TextView title = text("爪账 · 店员端", 16, Color.WHITE, Typeface.BOLD);
        bar.addView(title, new LinearLayout.LayoutParams(0, dp(48), 1));
        TextView refresh = action("刷新");
        TextView settings = action("换电脑");
        bar.addView(refresh, new LinearLayout.LayoutParams(dp(58), dp(48)));
        bar.addView(settings, new LinearLayout.LayoutParams(dp(72), dp(48)));
        root.addView(bar, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(48)));

        progress = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal);
        progress.setMax(100);
        progress.setProgressTintList(android.content.res.ColorStateList.valueOf(TEAL));
        root.addView(progress, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(3)));

        webView = new WebView(this);
        WebSettings webSettings = webView.getSettings();
        webSettings.setJavaScriptEnabled(true);
        webSettings.setDomStorageEnabled(true);
        webSettings.setDatabaseEnabled(true);
        webSettings.setBuiltInZoomControls(false);
        webSettings.setDisplayZoomControls(false);
        webSettings.setSupportZoom(false);
        webSettings.setAllowFileAccess(false);
        webSettings.setAllowContentAccess(false);
        webSettings.setJavaScriptCanOpenWindowsAutomatically(false);
        webSettings.setSafeBrowsingEnabled(true);
        webSettings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        CookieManager.getInstance().setAcceptCookie(true);
        CookieManager.getInstance().setAcceptThirdPartyCookies(webView, false);
        webView.setWebChromeClient(new android.webkit.WebChromeClient() {
            @Override public void onProgressChanged(WebView view, int newProgress) {
                progress.setProgress(newProgress);
                progress.setVisibility(newProgress >= 100 ? View.GONE : View.VISIBLE);
            }
        });
        webView.setWebViewClient(new WebViewClient() {
            @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri target = request.getUrl();
                Uri base = Uri.parse(serverUrl);
                if (isSameOrigin(target, base)) return false;
                try { startActivity(new Intent(Intent.ACTION_VIEW, target)); } catch (Exception ignored) { }
                return true;
            }
            @Override public void onReceivedError(WebView view, WebResourceRequest request, WebResourceError error) {
                if (request.isForMainFrame()) showConnectionError();
            }
            @Override public void onReceivedSslError(WebView view, SslErrorHandler handler, android.net.http.SslError error) {
                handler.cancel();
                Toast.makeText(MainActivity.this, "证书验证失败，已停止连接", Toast.LENGTH_LONG).show();
            }
        });
        root.addView(webView, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, 0, 1));
        refresh.setOnClickListener(v -> webView.reload());
        settings.setOnClickListener(v -> confirmChangeServer());
        setContentView(root);
        webView.loadUrl(serverUrl);
    }

    private void showConnectionError() {
        runOnUiThread(() -> {
            LinearLayout panel = vertical(20);
            panel.setPadding(dp(30), dp(64), dp(30), dp(24));
            panel.setGravity(Gravity.CENTER_HORIZONTAL);
            panel.addView(text("暂时连不上店内电脑", 23, INK, Typeface.BOLD), matchWrap());
            TextView help = text("确认电脑已启动爪账、手机仍连接门店 Wi‑Fi，并检查地址是否一致。", 15, MUTED, Typeface.NORMAL);
            help.setGravity(Gravity.CENTER); help.setLineSpacing(0, 1.25f);
            LinearLayout.LayoutParams helpLp = matchWrap(); helpLp.topMargin = dp(14);
            panel.addView(help, helpLp);
            Button retry = button("重新连接", TEAL, Color.WHITE);
            LinearLayout.LayoutParams retryLp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(54)); retryLp.topMargin = dp(28);
            panel.addView(retry, retryLp);
            Button change = button("修改电脑地址", Color.WHITE, NAVY);
            LinearLayout.LayoutParams changeLp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(54)); changeLp.topMargin = dp(12);
            panel.addView(change, changeLp);
            retry.setOnClickListener(v -> showBrowser());
            change.setOnClickListener(v -> showSetup());
            setContentView(panel);
        });
    }

    private void confirmChangeServer() {
        new AlertDialog.Builder(this)
            .setTitle("更换店内电脑？")
            .setMessage("只有电脑地址发生变化时才需要修改，登录状态可能需要重新验证。")
            .setNegativeButton("取消", null)
            .setPositiveButton("修改", (dialog, which) -> showSetup())
            .show();
    }

    private static boolean isSameOrigin(Uri target, Uri base) {
        return target.getScheme() != null
            && target.getHost() != null
            && target.getScheme().equalsIgnoreCase(base.getScheme())
            && target.getHost().equalsIgnoreCase(base.getHost())
            && effectivePort(target) == effectivePort(base);
    }

    private static int effectivePort(Uri uri) {
        if (uri.getPort() != -1) return uri.getPort();
        return "https".equalsIgnoreCase(uri.getScheme()) ? 443 : 80;
    }

    @Override public void onBackPressed() {
        if (webView != null && webView.canGoBack()) webView.goBack(); else super.onBackPressed();
    }

    private LinearLayout vertical(int spacing) {
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        layout.setBackgroundColor(Color.WHITE);
        if (spacing > 0) layout.setShowDividers(LinearLayout.SHOW_DIVIDER_MIDDLE);
        return layout;
    }
    private TextView text(String value, int size, int color, int style) {
        TextView view = new TextView(this); view.setText(value); view.setTextSize(size); view.setTextColor(color); view.setTypeface(Typeface.DEFAULT, style); return view;
    }
    private TextView action(String value) {
        TextView view = text(value, 14, Color.WHITE, Typeface.NORMAL); view.setGravity(Gravity.CENTER); view.setClickable(true); return view;
    }
    private Button button(String value, int background, int foreground) {
        Button button = new Button(this); button.setText(value); button.setTextSize(16); button.setTextColor(foreground); button.setBackgroundColor(background); return button;
    }
    private LinearLayout.LayoutParams wrap() { return new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT); }
    private LinearLayout.LayoutParams matchWrap() { return new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT); }
    private int dp(int value) { return Math.round(value * getResources().getDisplayMetrics().density); }
}
