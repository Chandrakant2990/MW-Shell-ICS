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
						url: "/api/tables/configuration",
						method: "GET",
						data: function (data) {
							if (data.order) {
								data.orderBy = data.columns[data.order[0].column].data;
								data.orderDir = data.order[0].dir;
							}
							delete data.columns;
							delete data.order;
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
							data: "partitionKey",
							type: "readonly"
						},
						{
							data: "rowKey",
							required: true,
							type: "text"
						},
						{ data: "configKey" },
						{
							data: "configValue",
							render: function (data, type, row, meta) {
								if (data) {
									return '<p class="config-text-format">' + data + '</p>';
								} else {
									return '';
								}
							}
						}
						/*and so on, keep adding data elements here for all your columns.*/
					],
					order: [[1, 'desc']],
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
						name: 'edit',      // do not change name
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
						titleAttr: 'Refresh the configuration table'
					}],
					onAddRow: function (datatable, rowdata, success, error) {
						if (!rowdata.partitionKey) {
							rowdata.partitionKey = "SPOCPI";
						}
						//// check for any html input
						if (SPOCPI.Common.checkForValidInput(rowdata.partitionKey) &&
							SPOCPI.Common.checkForValidInput(rowdata.rowKey) &&
							SPOCPI.Common.checkForValidInput(rowdata.configValue)) {
							$.ajax({
								url: "/api/tables/AddConfiguration",
								type: 'POST',
								headers: {
									"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
								},
								data: JSON.stringify({ PartitionKey: rowdata.partitionKey, RowKey: rowdata.rowKey, ConfigKey: rowdata.configKey, ConfigValue: rowdata.configValue }),
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
							url: "/api/tables/DeleteConfiguration",
							type: 'DELETE',
							data: JSON.stringify({ PartitionKey: rowdata.partitionKey, RowKey: rowdata.rowKey, ConfigKey: rowdata.configKey, ConfigValue: rowdata.configValue, ETag: "*" }),
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
							$.ajax({
								url: "/api/tables/AddConfiguration",
								type: 'POST',
								headers: {
									"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
								},
								data: JSON.stringify({ PartitionKey: rowdata.partitionKey, RowKey: rowdata.rowKey, ConfigKey: rowdata.configKey, ConfigValue: rowdata.configValue }),
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