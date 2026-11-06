package com.pawledger.staff;

import android.net.Uri;

final class ServerAddress {
    private ServerAddress() { }

    static String normalize(String raw) {
        String value = raw == null ? "" : raw.trim();
        if (value.isEmpty()) throw new IllegalArgumentException("请输入店内电脑地址");
        if (!value.contains("://")) value = "http://" + value;

        Uri uri = Uri.parse(value);
        String scheme = uri.getScheme();
        if (!("http".equalsIgnoreCase(scheme) || "https".equalsIgnoreCase(scheme))
                || uri.getHost() == null || uri.getHost().isBlank()
                || uri.getUserInfo() != null) {
            throw new IllegalArgumentException("请输入正确的电脑地址");
        }

        Uri.Builder builder = uri.buildUpon().fragment(null);
        if (uri.getPort() == -1 && "http".equalsIgnoreCase(scheme)) builder.encodedAuthority(uri.getHost() + ":5088");
        String path = uri.getPath();
        if (path == null || path.isBlank()) builder.path("/");
        return builder.build().toString();
    }
}
