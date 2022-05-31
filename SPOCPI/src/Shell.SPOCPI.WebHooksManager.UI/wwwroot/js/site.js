'use strict';

// Search CoE Class
var SearchCoE = SearchCoE || {};
SearchCoE.Srch = {
    MaxItemsPerPage: 500,
    MaxRowLimit: 500,
    MaxItemsExport: 1000,
    IconMappings: {
        "PowerPoint": "icpptx.svg",
        "odp": "icpptx.svg",
        "ppt": "icpptx.svg",
        "pptx": "icpptx.svg",
        "pptm": "icpptx.svg",
        "potm": "icpptx.svg",
        "potx": "icpptx.svg",
        "ppam": "icpptx.svg",
        "ppsm": "icpptx.svg",
        "ppsx": "icpptx.svg",

        "Word": "icdocx.svg",
        "docx": "icdocx.svg",
        "doc": "icdocx.svg",
        "docm": "icdocx.svg",
        "dot": "icdocx.svg",
        "nws": "icdocx.svg",
        "dotx": "icdocx.svg",

        "Visio": "icvsdx.svg",
        "vsdx": "icvsdx.svg",
        "vsd": "icvsdx.svg",
        "vsx": "icvsdx.svg",

        "Excel": "icxlsx.svg",
        "xlsx": "icxlsx.svg",
        "xls": "icxlsx.svg",
        "xlsb": "icxlsx.svg",
        "xlsm": "icxlsx.svg",
        "xltm": "icxlsx.svg",
        "xltx": "icxlsx.svg",
        "xlam": "icxlsx.svg",
        "odc": "icxlsx.svg",
        "ods": "icxlsx.svg",

        "OneNote": "icone.svg",
        "one": "icone.svg",

        "PDF": "icpdf.svg",
        "pdf": "icpdf.svg",

        "Image": "photo.svg",
        "bmp": "photo.svg",
        "jpg": "photo.svg",
        "jpeg": "photo.svg",
        "png": "photo.svg",
        "tiff": "photo.svg",
        "gif": "photo.svg",
        "rle": "photo.svg",
        "wmf": "photo.svg",
        "dib": "photo.svg",
        "ico": "photo.svg",
        "iwpd": "photo.svg",
        "odg": "photo.svg",

        "Video": "video.svg",

        "Email": "email.svg",
        "msg": "email.svg",
        "eml": "email.svg",
        "exch": "email.svg",

        "SharePoint Site": "site.svg",
        "Team Site": "site.svg",

        "Web page": "html.svg",
        "aspx": "html.svg",
        "html": "html.svg",
        "mhtml": "html.svg",
        "htm": "html.svg",

        "txt": "genericfile.svg",
        "csv": "genericfile.svg",
        "tsv": "genericfile.svg",
        "json": "genericfile.svg",

        "Zip": "iczip.svg",
        "zip": "iczip.svg",
        "rar": "iczip.svg",

        "newtab": "newtab.png",
        "Folder": "folder.svg",
        "Yammer": "Yammer.svg",
        "Stream": "Stream.svg",

        "All": "genericfile.svg"
    },
    FindInitials: function (str) {
        var splitStr = str.toLowerCase().split(' ');
        var maxCaps = splitStr.length > 3 ? 3 : splitStr.length;
        for (var i = 0; i < splitStr.length; i++) {
            splitStr[i] = i < maxCaps ? splitStr[i].charAt(0).toUpperCase() : "";
        }
        // Directly return the joined string
        return splitStr.join('').trim();
    },
    PickColor: function () {
        var colors = ["#FFCD00", "#DA291C", "#002F6C", "#C4D600", "#642667", "#ED8B00", "#77C5D5", "#00843D", "#FFCD00", "#D42E12", "#F7D117", "#174E82", "#BDBDBD"];
        var random_color = colors[Math.floor(Math.random() * colors.length)];
        return random_color;
    },
    EscapeString: function (unescapedString) {
        var escapedString = unescapedString;
        if (unescapedString !== null) {
            escapedString = unescapedString.replace(/\\/g, "\\\\");
            escapedString = escapedString.replace(/\'/g, "\\\'");
            escapedString = escapedString.replace(/\r?\n|\r/g, "");
            escapedString = escapedString.replace(/,/g, "");
        }
        return escapedString;
    },
    MapLabelToIconFilename: function (label) {
        var IconFileName = "genericfile.svg";
        label = label.replace(/\./g, "");

        if (label in this.IconMappings) {
            IconFileName = this.IconMappings[label];
        }
        return "/icons/" + IconFileName;
    },
    OpenInNewTab: function (url) {
        window.open(url, '_blank');
    },
    GetUrlVars: function (url) {
        var vars = {};
        var parts = url.replace(/[?&#]+([^=&#]+)=([^&^#]*)/gi,
            function (m, key, value) {
                vars[key] = value;
            });
        return vars;
    }
};

// SPOCPI common class
var SPOCPI = SPOCPI || {};
SPOCPI.Common = {
    formatValue: function (value) {
        if (value) {
            return value;
        } else {
            try {
                if (!value) {
                    return '';
                }
                else {
                    value = parseInt(value, 10);
                    return value;
                }
            } catch (e) {
                console.error(e);
                return '';
            }
        }
    },
    formatDateTime: function (value) {
        if (value) {
            return moment.utc(value).format();
        } else {
            return '';
        }
    },
    checkForValidInput: function (value) {
        if (value) {
            var regex = /<("[^"]*"|'[^']*'|[^'">])*>/;
            if (regex.test(value)) {
                return false;
            } else {
                return true;
            }
        } else {
            return true;
        }
    },
    stripTrailingSlash: function (value){
        value = $.trim(value);
        return (value.endsWith('/') ? value.slice(0, -1) : value);
    },
    checkForConfidentialSite: function (value, valueToRestrict) {
        value = $.trim(value);
        value = value.endsWith('/') ? value.slice(0, -1) : value;
        let array = valueToRestrict.split(';');
        return (array.some(v => (value.substring(value.lastIndexOf('/') + 1).toLowerCase()).match("^" + v.toLowerCase())))
    }, 
    invalidUrl: function () {
        $('#dangerModal').modal({
            keyboard: false
        });
        $('.modal-body').html("Invalid url detected");
    },
    stringToBoolean: function (string) {
        switch (string.toLowerCase().trim()) {
            case "true": case "yes": case "1": return true;
            case "false": case "no": case "0": case null: return false;
            default: return "InValidInput";
        }
    }
};