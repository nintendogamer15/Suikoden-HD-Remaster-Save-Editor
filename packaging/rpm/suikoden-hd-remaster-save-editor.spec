%{!?app_version:%global app_version 0.0.0}
%global debug_package %{nil}
%global _build_id_links none
%global __strip /bin/true
%global source_date_epoch_from_changelog 0
%global appdir %{_prefix}/lib/%{name}
# .NET ships this optional diagnostics provider linked to the retired
# liblttng-ust.so.0 ABI. The editor does not load it during normal execution,
# and current Fedora provides only liblttng-ust.so.1.
%global __requires_exclude ^liblttng-ust\\.so\\.0.*$

Name:           suikoden-hd-remaster-save-editor
Version:        %{app_version}
Release:        1
Summary:        Save editor for Suikoden I and II HD Remaster
License:        0BSD AND MIT AND BSD-3-Clause AND OFL-1.1
URL:            https://github.com/nintendogamer15/Suikoden-HD-Remaster-Save-Editor
Source0:        app-bundle.tar.gz
Source1:        suikoden-hd-remaster-save-editor.desktop
Source2:        suikoden-hd-remaster-save-editor.svg

ExclusiveArch:  x86_64
BuildRequires:  desktop-file-utils
BuildRequires:  libxml2
Requires:       fontconfig
Requires:       dejavu-sans-fonts
Requires:       glibc
Requires:       hicolor-icon-theme
Requires:       krb5-libs
Requires:       libgcc
Requires:       libICE
Requires:       libicu
Requires:       libSM
Requires:       libstdc++
Requires:       libX11
Requires:       openssl-libs
Requires:       tzdata
Requires:       zlib-ng-compat
Recommends:     xdg-desktop-portal

%description
A self-contained Avalonia desktop editor that opens, validates, and safely
edits encrypted PC saves for Suikoden I and Suikoden II HD Remaster.

%prep

%build

%install
install -d %{buildroot}%{appdir}
tar -xzf %{SOURCE0} --strip-components=1 -C %{buildroot}%{appdir}
chmod 0755 %{buildroot}%{appdir}/SuikodenHdSaveEditor.App

install -d %{buildroot}%{_bindir}
ln -s %{appdir}/SuikodenHdSaveEditor.App \
  %{buildroot}%{_bindir}/suikoden-hd-remaster-save-editor
desktop-file-install --dir=%{buildroot}%{_datadir}/applications %{SOURCE1}
install -Dm0644 %{SOURCE2} \
  %{buildroot}%{_datadir}/icons/hicolor/scalable/apps/suikoden-hd-remaster-save-editor.svg

install -Dm0644 %{buildroot}%{appdir}/LICENSE \
  %{buildroot}%{_licensedir}/%{name}/LICENSE
install -d %{buildroot}%{_licensedir}/%{name}/LICENSES
cp -a %{buildroot}%{appdir}/LICENSES/. \
  %{buildroot}%{_licensedir}/%{name}/LICENSES/
install -Dm0644 %{buildroot}%{appdir}/THIRD_PARTY_NOTICES.md \
  %{buildroot}%{_docdir}/%{name}/THIRD_PARTY_NOTICES.md
install -Dm0644 %{buildroot}%{appdir}/README.md \
  %{buildroot}%{_docdir}/%{name}/README.md

%check
desktop-file-validate \
  %{buildroot}%{_datadir}/applications/suikoden-hd-remaster-save-editor.desktop
xmllint --noout \
  %{buildroot}%{_datadir}/icons/hicolor/scalable/apps/suikoden-hd-remaster-save-editor.svg

%files
%{appdir}/
%{_bindir}/suikoden-hd-remaster-save-editor
%{_datadir}/applications/suikoden-hd-remaster-save-editor.desktop
%{_datadir}/icons/hicolor/scalable/apps/suikoden-hd-remaster-save-editor.svg
%license %{_licensedir}/%{name}/LICENSE
%license %{_licensedir}/%{name}/LICENSES/*
%doc %{_docdir}/%{name}/THIRD_PARTY_NOTICES.md
%doc %{_docdir}/%{name}/README.md

%changelog
* Tue Aug 25 2026 Robert - %{app_version}-1
- Add native Fedora packaging for the self-contained application.
