'use strict';

var SPOCPI = SPOCPI || {};

SPOCPI.Configuration = {
	load: function () {
		var queryText = "";
		var validRequest = true;
		var params = (new URL(document.location)).searchParams;
		var driveId = params.get("driveId");
		if (driveId) {
			if (SPOCPI.Common.checkForValidInput(driveId)) {
				driveId = decodeURIComponent(driveId);
			} else {
				validRequest = false;
				SPOCPI.Common.invalidUrl();
			}
		}
		if (validRequest) {
			$(document).ready(function () {
				var table = $('#trackingTable').DataTable({
					dom: 'Bfrtip',
					orderMulti: false,
					serverSide: false,
					processing: true,
					searching: true,
					initComplete: function () { SPOCPI.Configuration.onint(); },
					ajax: {
						url: "/api/tables/notification",
						method: "GET",
						data: function (data) {
							if (data.order) {
								data.orderBy = data.columns[data.order[0].column].data;
								data.orderDir = data.order[0].dir;
							}
							delete data.columns;
							delete data.order;
						},
						dataSrc: function (json) {
							for (var i = 0; i < json.data.length; i++) {
								json.data[i].messageJson = JSON.parse(json.data[i].messageJson);
							}
							return json.data;
						},
						error: function (xhr, textStatus, errorThrown) {
							try {
								var message = xhr.responseText;
								if (message) {
									$('#dangerModal').modal({
										keyboard: false
									});
									$('.modal-body').html(message);
								}
							} catch (e) {
								console.error(e);
							}
						}
					},
					pageLength: 50,
					columns: [
						{
							title: "Partition Key",
							data: "partitionKey"
						},
						{
							title: "Row Key",
							data: "rowKey"
						},
						{
							title: "Attempts Count",
							data: "attemptsCount"
						},
						{
							title: "Tenant Id",
							data: "messageJson.tenantId"
						},

						{
							title: "Subscription Id",
							data: "messageJson.subscriptionId"
						},
						{
							"title": "SPO Subscription Id",
							data: "messageJson.clientState"

						},
						{
							title: "Change Type",
							data: "messageJson.changeType"
						},
						{
							data: "receivedTime",
							render: function (data, type, row) {
								if (data) {
									return moment.utc(data).format();
								}
								else
									return "";
							}
						},
						{ data: "status" }
					],
					"order": [[7, 'desc']],
					select: 'single',
					altEditor: true,
					buttons: [{
						text: 'Add',
						name: 'add',        // do not change name
						titleAttr: 'Add a new item'
					},
					{
						extend: 'selected', // Bind to Selected row
						text: 'Edit',
						name: 'edit',        // do not change name
						titleAttr: 'Edit the selected item'
					},
					{
						extend: 'selected', // Bind to Selected row
						text: 'Delete',
						name: 'delete',      // do not change name
						titleAttr: 'Delete the selected item'
					},
					{
						text: 'Refresh',
						name: 'refresh',      // do not change name
						titleAttr: 'Refresh the Notification table'
					},
					{
						extend: 'copy',
						text: 'Copy',
						exportOptions: {
							modifier: {
								page: 'all'
							},
							columns: ':not(.notexport)'
						},
						titleAttr: 'Copy the current table in the clipboard'
					},
					{
						text: 'Export CSV',
						extend: 'csv',
						exportOptions: {
							modifier: {
								page: 'all'
							},
							columns: ':not(.notexport)'
						},
						titleAttr: 'Export the current table in CSV format'
					},
					{
						text: 'Export Excel',
						extend: 'excel',
						exportOptions: {
							modifier: {
								page: 'all'
							},
							columns: ':not(.notexport)'
						},
						titleAttr: 'Export the current table in Excel format'
					}],
					onAddRow: function (datatable, rowdata, success, error) {
						//// check for any html input
						if (SPOCPI.Common.checkForValidInput(rowdata.partitionKey) &&
							SPOCPI.Common.checkForValidInput(rowdata.rowKey) &&
							SPOCPI.Common.checkForValidInput(rowdata.configValue)) {
							var messageJson = JSON.stringify(
								{
									"subscriptionId": rowdata["messageJson.subscriptionId"],
									"clientState": rowdata["messageJson.clientState"],
									"tenantId": rowdata["messageJson.tenantId"],
									"changeType": rowdata["messageJson.changeType"],
									"subscriptionExpirationDateTime": null,
									"resource": null,
									"resourceData": null
								});
							var attemptsCountInt = parseInt(rowdata.attemptsCount);
							var receivedTime = new Date(rowdata.receivedTime);
							$.ajax({
								url: "/api/tables/AddNotification",
								type: 'POST',
								headers: {
									"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
								},
								data: JSON.stringify({ PartitionKey: rowdata.partitionKey, RowKey: rowdata.rowKey, AttemptsCount: attemptsCountInt, MessageJson: messageJson, ReceivedTime: receivedTime, Status: rowdata.status }),
								dataType: "json",
								contentType: 'application/json; charset=utf-8',
								success: success,
								error: error
							});
						} else {
							$('#dangerModal').modal({
								keyboard: false
							});
							$('.modal-body').html("Invalid input detected");
						}
					},
					onDeleteRow: function (datatable, rowdata, success, error) {
						$.ajax({
							url: "/api/tables/DeleteNotification",
							type: 'DELETE',
							data: JSON.stringify({ PartitionKey: rowdata.partitionKey, RowKey: rowdata.rowKey, AttemptsCount: rowdata.attemptsCount, MessageJson: rowdata.messageJson, ReceivedTime: rowdata.receivedTime, Status: rowdata.status, ETag: "*" }),
							dataType: "json",
							contentType: 'application/json; charset=utf-8',
							success: success,
							error: error
						});
					},
					onEditRow: function (datatable, rowdata, success, error) {
						//// check for any html input
						if (SPOCPI.Common.checkForValidInput(rowdata.partitionKey) &&
							SPOCPI.Common.checkForValidInput(rowdata.rowKey) &&
							SPOCPI.Common.checkForValidInput(rowdata.configValue)) {
							var messageJson = JSON.stringify(
								{
									"subscriptionId": rowdata["messageJson.subscriptionId"],
									"clientState": rowdata["messageJson.clientState"],
									"tenantId": rowdata["messageJson.tenantId"],
									"changeType": rowdata["messageJson.changeType"],
									"subscriptionExpirationDateTime": null,
									"resource": null,
									"resourceData": null
								});
							var attemptsCountInt = parseInt(rowdata.attemptsCount);
							$.ajax({
								url: "/api/tables/AddNotification",
								type: 'POST',
								headers: {
									"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
								},
								data: JSON.stringify({ PartitionKey: rowdata.partitionKey, RowKey: rowdata.rowKey, AttemptsCount: attemptsCountInt, MessageJson: messageJson, ReceivedTime: rowdata.receivedTime, Status: rowdata.status, ETag: "*" }),
								dataType: "json",
								contentType: 'application/json; charset=utf-8',
								success: success,
								error: error
							});
							console.log(error);
						} else {
							$('#dangerModal').modal({
								keyboard: false
							});
							$('.modal-body').html("Invalid input detected");
						}
					}
				});

				var isAdmin = SPOCPI.Configuration.loggedInUserIsAdmin();

				table.on('select', function () {
					try {
						SPOCPI.Configuration.disableActionButtons(isAdmin, [1, 2]);
					} catch (e) {
						console.error(e);
						table.button(1).enable(false);
						table.button(2).enable(false);
					}
				});

				SPOCPI.Configuration.disableActionButtons(isAdmin, [0, 1, 2]);
			});
		}
	},
	onint: function () {
		// take off all events from the searchfield
		$("#trackingTable_wrapper input[type='search']").off();
		// Use return key to trigger search
		$("#trackingTable_wrapper input[type='search']").on("keydown", function (evt) {
			if (evt.keyCode === 13) {
				$("#trackingTable").DataTable().search($("input[type='search']").val()).draw();
			}
		});
		$("#search-btn").button().on("click", function () {
			$("#trackingTable").DataTable().search($("input[type='search']").val()).draw();
		});
	},
	loggedInUserIsAdmin: function () {
		var userIsAdmin = $('#isAdmin').val();
		if (userIsAdmin) {
			try {
				userIsAdmin = JSON.parse(userIsAdmin);
			}
			catch (e) {
				userIsAdmin = false;
			}
		} else {
			userIsAdmin = false;
		}

		return userIsAdmin;
	},
	disableActionButtons: function (userIsAdmin, btnArrayToDisableEnable) {
		var table = $("#trackingTable").DataTable();
		if (table && btnArrayToDisableEnable && btnArrayToDisableEnable.length > 0) {
			for (var i = 0; i < btnArrayToDisableEnable.length; i++) {
				table.button(btnArrayToDisableEnable[i]).enable(userIsAdmin);
			}
		}
	}
};

SPOCPI.Configuration.load();