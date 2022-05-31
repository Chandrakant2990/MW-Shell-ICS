'use strict';

var SPOCPI = SPOCPI || {};

SPOCPI.Tracking = {
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
					serverSide: true,
					processing: true,
					searching: true,
					initComplete: function () { SPOCPI.Tracking.onint(); },
					ajax: {
						url: "/api/tracking/search",
						method: "GET",
						data: function (data) {
							data.driveId = driveId;
							data.orderBy = data.columns[data.order[0].column].data;
							data.orderDir = data.order[0].dir;
							delete data.columns;
							delete data.order;
						},
						error: function (xhr, textStatus, errorThrown) {
							try {
								var message = xhr.responseText;
								if (!message) {
									message = "An unexpected error occurred. Please contact your administrator.";
								}
								$('#dangerModal').modal({
									keyboard: false
								});
								$('.modal-body').html(message);
							} catch (e) {
								console.error(e);
							}
						}
					},
					lengthMenu: [
						[10, 25, 50, 100],
						['10 rows', '25 rows', '50 rows', '100 rows']
					],
					pageLength: 25,
					columns: [
						{
							"className": 'details-control',
							"orderable": false,
							"data": null,
							"defaultContent": ''
						},
						{
							data: "document.extension",
							"orderable": false,
							render: function (data, type, row) {
								var iconPath = SearchCoE.Srch.MapLabelToIconFilename("All");
								if (data) {
									iconPath = SearchCoE.Srch.MapLabelToIconFilename(data);
								}
								var resultsHtml = '<div class="ms-CommandButton-icon">';
								resultsHtml += '    <i class="html-icon ms-Icon">';
								resultsHtml += '        <img style="width: 32px;height: 32px;" src="' + iconPath + '"/>';
								resultsHtml += '    </i>';
								resultsHtml += '</div>';
								return resultsHtml;
							}
						},
						{ data: "document.rowKey" },
						{ data: "document.name" },
						{
							"className": 'url-cell',
							"data": "document.webUrl",
							"render": function (data, type, row, meta) {
								if (type === 'display') {
									data = '<a target="_blank" href="' + data + '">' + data + '</a>';
								}
								return data;
							}
						},
						{
							data: "document.timestamp",
							render: function (data, type, row) {
								if (data) {
									return moment.utc(data).format();
								}
								else
									return "";
							}
						},
						{ data: "document.isFolder" },
						{ data: "document.documentCTagChange" },
						{ data: "document.documentETagChange" },
						{ data: "document.partitionKey", visible: false },
						{ data: "document.rowKey", visible: false },
						{ data: "document.siteId", visible: false },
						{ data: "document.webId", visible: false },
						{ data: "document.listId", visible: false },
						{ data: "document.listItemId", visible: false }
					],
					order: [[5, 'desc']],
					buttons: [
						{
							extend: 'pageLength',
							titleAttr: 'Select number of items to display per page'
						},
						{
							text: 'Refresh',
							titleAttr: 'Refresh the Tracking table',
							action: function (e, dt, node, config) {
								dt.ajax.reload(null, false);
							}
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
						},
					]
				});

				$('#trackingTable tbody').on('click', 'td.details-control', function () {
					var tr = $(this).closest('tr');
					var row = table.row(tr);

					if (row.child.isShown()) {
						// This row is already open - close it
						row.child.hide();
						tr.removeClass('shown');
					}
					else {
						// Open this row
						row.child(SPOCPI.Tracking.expandCollapseDetail(row.data())).show();
						tr.addClass('shown');
					}
				});
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
	expandCollapseDetail: function (d) {
		var output = '';
		output += '<div class="box box-solid">';
		output += ' <div class="box-body clearfix">';
		output += '<dl class="dl-horizontal">';
		output += '<dt title="Partition Key">Partition Key</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.partitionKey) + '</dd>';
		output += '<dt title="Row Key">Row Key</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.rowKey) + '</dd>';
		output += '<dt title="List Id">List Id</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.listId) + '</dd>';
		output += '<dt title="ETag">ETag</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.documentETag) + '</dd>';
		output += '<dt title="CTag">CTag</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.documentCTag) + '</dd>';
		output += '<dt title="List Item Id">List Item Id</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.listItemId) + '</dd>';
		output += '<dt title="Web Id">Web Id</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.webId) + '</dd>';
		output += '<dt title="Site Id">Site Id</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.siteId) + '</dd>';
		output += '<dt title="Parent Folder Url">Parent Folder Url</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.parentFolderUrl) + '</dd>';
		output += '</dl>';
		output += '</div>';
		output += '</div>';

		return output;
	}
};

SPOCPI.Tracking.load();