'use strict';

var SPOCPI = SPOCPI || {};

SPOCPI.Redis = {
    RefreshCache: function () {
        $.ajax({
            url: "/Redis/RefreshCache?key=" + $('#txtClearCache').val(),
            type: "GET",
            success: function (response) {
                SPOCPI.Redis.ShowResponse(response);
            }
        });
    },
    ShowCache: function () {
        $.ajax({
            url: "/Redis/ShowCache?key=" + $('#txtClearCache').val(),
            type: "GET",
            success: function (response) {
                SPOCPI.Redis.ShowResponse(response);
            }
        });
    },
    ShowResponse: function (response) {
        $("#divResult").empty();
        if (response.error != null) {
            alert(response.error);
        }
        if (response.status != null) {
            alert(response.status);
        }
        if (response.data != null) {
            var datatoshow = '<table id="redisCacheTable" summary="Redis Table" class="table table-bordered table-striped dataTable" width="100%"><tr> <th style="text-align: center" scope="col">Config Key</th><th style="text-align: center" scope="col">Config Value</th></tr >' + $.map(response.data, function (value, key) {
                return '<tr><td> ' + key + '</td><td>' + value + '</td></tr>'
            }).join('') + '</table>'
            $("#divResult").html(datatoshow);
        }
    }
}