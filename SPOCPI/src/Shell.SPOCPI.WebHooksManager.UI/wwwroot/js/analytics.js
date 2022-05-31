'use strict';

var SPOCPI = SPOCPI || {};

SPOCPI.Analytics = {
	sitesTree: null,
	dataTable: null,
	selectedSiteIdsLocalStorageKey: "SPOCPI_SelectedSiteIds",
	selectedSiteUrlsLocalStorageKey: "SPOCPI_SelectedSiteUrls",
	selectedLibraryUrlsLocalStorageKey: "SPOCPI_SelectedLibraryUrls",
	requestObj: {
		siteUrls: [],
		libraryUrls: [],
		fetchList: ["az", "sp", "sites", "common"]
	},
	init: function () {
		// Populate site and library urls from local storage (if any).
		SPOCPI.Analytics.intializeRequestObject();

		$.ajax({
			type: "POST",
			url: "/api/analytics",
			data: JSON.stringify(SPOCPI.Analytics.requestObj),
			dataType: "json",
			contentType: 'application/json; charset=utf-8',
			success: function (data) {
				SPOCPI.Analytics.processCommonMetricsInfo(data.common);
				SPOCPI.Analytics.loadSitesTree(data.sites);
				SPOCPI.Analytics.loadDataTable(data);

				// Hide spinner and show the analytics information
				$("#divSpinner").removeClass("show").addClass("hide");
				$(".nav-tabs-custom").removeClass("hide").addClass("show");

				// Click event for apply button
				$("#selectSitesApply").off('click').on('click', function (e) {
					// Show Spinner and Hide the table.
					$("#divSpinner").removeClass("hide").addClass("show");
					$(".nav-tabs-custom").removeClass("show").addClass("hide");


					SPOCPI.Analytics.applySitesAndLibraryClickEvent(e);
					e.stopPropagation()
				});
			},
			error: function (xhr, textStatus, errorThrown) {
				try {

				} catch (e) {
					console.error(e);
				}
			}
		});
	},
	intializeRequestObject: function () {
		var siteUrls = window.sessionStorage.getItem(SPOCPI.Analytics.selectedSiteUrlsLocalStorageKey);
		var libraryUrls = window.sessionStorage.getItem(SPOCPI.Analytics.selectedLibraryUrlsLocalStorageKey);

		if (siteUrls != null && siteUrls != "") {
			SPOCPI.Analytics.requestObj.siteUrls = siteUrls.split(';');
		}

		if (libraryUrls != null && libraryUrls != "") {
			SPOCPI.Analytics.requestObj.libraryUrls = libraryUrls.split(';');
		}
	},
	processCommonMetricsInfo: function (commonData) {
		SPOCPI.Analytics.bindCommonMetricsValue(commonData, "SubscriptionTrackingStatus", "subscriptionTrackingTotalCount", "subscriptionTrackingLast1", "subscriptionTrackingLast24", "subscriptionTrackingLastRefresh");
		SPOCPI.Analytics.bindCommonMetricsValue(commonData, "DocumentTrackingStatus", "documentTrackingTotalCount", "documentTrackingLast1", "documentTrackingLast24", "documentTrackingLastRefresh");
	},
	bindCommonMetricsValue: function (commonData, partitionKey, colTotalCount, colLast1Hr, colLast24Hrs, colLastRefresh) {
		var filteredElement = commonData.filter(function (a) {
			return a.document.partitionKey === partitionKey;
		});
		if (filteredElement.length > 0) {
			$("#" + colTotalCount).html(filteredElement[0].document.totalCount ?? 0);
			$("#" + colLast1Hr).html(filteredElement[0].document.last01Hours);
			$("#" + colLast24Hrs).html(filteredElement[0].document.last24Hours);
			$("#" + colLastRefresh).html(new Date(filteredElement[0].document.timestamp).toLocaleString());
		}
	},
	loadSitesTree: function (sitesJsonArray) {
		jQuery(document).ready(function () {
			// Populate sites and library urls from local storage.
			SPOCPI.Analytics.getSelectedTreeNodes();

			SPOCPI.Analytics.sitesTree = jQuery('#sitesDropdownMenu').tree({
				uiLibrary: 'bootstrap4',
				checkboxes: true,
				dataSource: JSON.parse(sitesJsonArray),
				checked: true,
				selectionType: 'multiple',
				autoLoad: true,
				initialized: function (e) {
					$("#sitesDropdownMenu").removeClass("dropdown-menu");
				}
			});

			// Set the default selection of user.
			if (window.sessionStorage.getItem(SPOCPI.Analytics.selectedSiteIdsLocalStorageKey) == null) {
				SPOCPI.Analytics.sitesTree.checkAll();
			} else {
				SPOCPI.Analytics.sitesTree.uncheckAll();
				window.sessionStorage.getItem(SPOCPI.Analytics.selectedSiteIdsLocalStorageKey).split(",").map(i => Number(i)).forEach(function (nodeToCheck) {
					SPOCPI.Analytics.sitesTree.check(SPOCPI.Analytics.sitesTree.getNodeById(nodeToCheck))
				});
			}
		});
	},
	loadDataTable: function (analyticsResponse) {
		var data = [];
		for (var i = 0; i < analyticsResponse.azure.length; i++) {
			var dataObj = {};
			dataObj["SiteUrl"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.azure[i], "siteUrl");
			dataObj["LibraryUrl"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.azure[i], "libraryUrl");
			dataObj["SPItemCount"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.sharePoint[i], "totalRowCount");
			dataObj["AZItemCount"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.azure[i], "totalRowCount");
			dataObj["SPFileType"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.sharePoint[i], "fileType");
			dataObj["AZFileType"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.azure[i], "extension");
			dataObj["SPTimestamp"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.sharePoint[i], "timestamp");
			dataObj["AZTimestamp"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.azure[i], "timestamp");
			dataObj["PartitionKey"] = SPOCPI.Analytics.getAnalyticsResponseValue(analyticsResponse.azure[i], "partitionKey");
			data.push(dataObj);
		}

		$(document).ready(function () {
			SPOCPI.Analytics.dataTable = $('#statsTable').DataTable({
				dom: 'Bfrtip',
				data: data,
				searching: true,
				lengthMenu: [
					[10, 25, 50, 100],
					['10 rows', '25 rows', '50 rows', '100 rows']
				],
				pageLength: 25,
				columns: [
					{
						className: 'details-control',
						orderable: false,
						data: null,
						defaultContent: ''
					},
					{ data: 'SiteUrl' },
					{ data: 'LibraryUrl' },
					{ data: 'SPItemCount', visible: false },
					{ data: 'AZItemCount', visible: false },
					{ data: 'SPFileType', visible: false },
					{ data: 'AZFileType', visible: false },
					{ data: 'SPTimestamp', visible: false },
					{ data: 'AZTimestamp', visible: false }
				],
				buttons: [
					{
						extend: 'copy',
						text: 'Copy',
						exportOptions: {
							modifier: {
								page: 'all'
							}
						},
						titleAttr: 'Copy the current table in the clipboard'
					},
					{
						text: 'Export CSV',
						extend: 'csv',
						exportOptions: {
							modifier: {
								page: 'all'
							}
						},
						titleAttr: 'Export the current table in CSV format'
					},
					{
						text: 'Export Excel',
						extend: 'excel',
						exportOptions: {
							modifier: {
								page: 'all'
							}
						},
						titleAttr: 'Export the current table in Excel format'
					},
					{
						extend: 'pageLength',
						titleAttr: 'Select number of items to display per page'
					},
				]
			});

			$('#statsTable tbody').off('click').on('click', 'td.details-control', function () {
				var tr = $(this).closest('tr');
				var row = SPOCPI.Analytics.dataTable.row(tr);

				if (row.child.isShown()) {
					// This row is already open - close it
					row.child.hide();
					tr.removeClass('shown');
				}
				else {
					// Open this row
					row.child(SPOCPI.Analytics.expandCollapseDetail(row[0][0], row.data())).show();
					tr.addClass('shown');
				}
			});
		});
	},
	expandCollapseDetail: function format(rowIndex, d) {
		var tabHtml = '<div class="nav-tabs-custom" style="padding-left: 75px;">';
		tabHtml += '<ul class="nav nav-tabs"><li class="active" ><a href="#tab_itemCount_' + rowIndex + '" data-toggle="tab" aria-expanded="true">Item Count</a></li><li class=""><a href="#tab_fileType_' + rowIndex + '" data-toggle="tab" aria-expanded="false">File Type</a></li><li class=""><a href="#tab_timestamp_' + rowIndex + '" data-toggle="tab" aria-expanded="false">Last Refresh Time</a></li></ul>';
		tabHtml += '<div class="tab-content"><div class="tab-pane active" id="tab_itemCount_' + rowIndex + '">{0}</div><div class="tab-pane" id="tab_fileType_' + rowIndex + '">{0}</div><div class="tab-pane" id="tab_timestamp_' + rowIndex + '">{0}</div>';
		tabHtml += '</div>';

		var hiddenDivId = 'hiddenRefinerDiv_' + rowIndex;
		$('<div>').attr({
			type: 'hidden',
			id: hiddenDivId,
		}).appendTo('body');

		$("#" + hiddenDivId).html(tabHtml);

		$("#tab_itemCount_" + rowIndex).html(SPOCPI.Analytics.getTableHtmlForRefiner("ItemCount", d.SPItemCount, d.AZItemCount, false));
		$("#tab_fileType_" + rowIndex).html(SPOCPI.Analytics.getTableHtmlForRefiner("File Type", d.SPFileType, d.AZFileType, true));
		$("#tab_timestamp_" + rowIndex).html(SPOCPI.Analytics.getTableHtmlForRefiner("Timestamp", d.SPTimestamp, d.AZTimestamp, false));

		tabHtml = $("#" + hiddenDivId).html();
		$("#" + hiddenDivId).remove();
		return tabHtml;
	},
	getTableHtmlForRefiner: function (label, spColValue, azColValue, isJsonString) {
		spColValue = isJsonString ? this.getTableHtmlForJsonString(spColValue) : spColValue;
		azColValue = isJsonString ? this.getTableHtmlForJsonString(azColValue) : azColValue;

		var html = '<table id="' + label + '_statsTable" class="table table-bordered table-striped dataTable" width="100%">';
		html += '<thead><tr><th scope="col">SharePoint</th><th scope="col">Azure</th></tr></thead>';
		html += '<tr><td width="50%">' + spColValue + '</td><td width="50%">' + azColValue + '</td></tr>';
		html += '</table>';
		return html;
	},
	getTableHtmlForJsonString: function (stringValue) {
		var tableHtml = "<table class='table table-bordered table-striped no-footer'><tbody>";
		if (stringValue != null) {
			var jsonArray = JSON.parse(stringValue);
			if (jsonArray.length > 0) {
				jsonArray.forEach(function (json) {
					var jsonValue = json.Value;
					if (typeof (jsonValue) == "string") {
						if (jsonValue === "") {
							jsonValue = "folder"
						}

						if (jsonValue.indexOf(".") == 0) {
							jsonValue = jsonValue.substring(1);
						}
					}

					tableHtml += "<tr><td width='50%'>" + jsonValue + "</td><td width='50%'>" + json.Count + "</td></tr>";
				});
			} else {
				tableHtml += "<tr><td>No data available</td></tr>";
			}
			tableHtml += "</tbody></table>";
		} else {
			tableHtml += "<tr><td>No data available</td></tr></tbody></table>";
		}
		return tableHtml;
	},
	applySitesAndLibraryClickEvent: function (e) {
		var selectedNodeIds = SPOCPI.Analytics.getSelectedTreeNodes(e);

		// Reset the site and library url arrays
		SPOCPI.Analytics.requestObj.siteUrls = [];
		SPOCPI.Analytics.requestObj.libraryUrls = [];

		if (selectedNodeIds.indexOf(1) != 0) {
			// Site Array - Level 2
			// Library Array - Level 3
			selectedNodeIds.forEach(function (selectedNodeId) {
				var selectedNodeText = SPOCPI.Analytics.sitesTree.getDataById(selectedNodeId).text.toLowerCase();
				var selectedNodeLevel = SPOCPI.Analytics.sitesTree.getDataById(selectedNodeId).level;

				if (selectedNodeLevel == 2 && SPOCPI.Analytics.requestObj.siteUrls.indexOf(selectedNodeText) == -1) {
					SPOCPI.Analytics.requestObj.siteUrls.push(selectedNodeText);

					// Remove the corresponding library nodes for this site. 
					var level3NodeIdsList = SPOCPI.Analytics.sitesTree.getChildren(SPOCPI.Analytics.sitesTree.getNodeById(selectedNodeId));
					level3NodeIdsList.forEach(function (level3NodeId) {
						var level3NodeIdIndex = selectedNodeIds.indexOf(level3NodeId);
						selectedNodeIds.splice(level3NodeIdIndex, 1);
					});
				}
				else if (selectedNodeLevel == 3) {
					var parentNodeId = SPOCPI.Analytics.getParentNodeId(selectedNodeId);
					var parentNodeText = SPOCPI.Analytics.sitesTree.getDataById(parentNodeId).text.toLowerCase();

					// Check if Site is already in the sites list, else add it to library list.
					if (SPOCPI.Analytics.requestObj.siteUrls.indexOf(parentNodeText) == -1) {
						SPOCPI.Analytics.requestObj.libraryUrls.push(selectedNodeText);
					}
				}
			});
		}
		else {
			// All Selected. Do nothing.
		}

		// Store the selected node id's in local storage.
		window.sessionStorage.setItem(SPOCPI.Analytics.selectedSiteIdsLocalStorageKey, selectedNodeIds);

		// Store the selected site and library urls in local storage.
		window.sessionStorage.setItem(SPOCPI.Analytics.selectedSiteUrlsLocalStorageKey, SPOCPI.Analytics.requestObj.siteUrls.join(';'));
		window.sessionStorage.setItem(SPOCPI.Analytics.selectedLibraryUrlsLocalStorageKey, SPOCPI.Analytics.requestObj.libraryUrls.join(';'));

		// Destroy old table and tree.
		SPOCPI.Analytics.sitesTree.destroy();
		SPOCPI.Analytics.dataTable.destroy();
		SPOCPI.Analytics.init();
	},
	getParentNodeId: function (nodeId) {
		return parseInt(SPOCPI.Analytics.sitesTree.getNodeById(nodeId).closest("ul").closest("li").attr("data-id"));
	},
	getSelectedTreeNodes: function (e) {
		// Set default as all selected.
		var checkedNodeIds = [1];
		if (e != undefined) {
			checkedNodeIds = SPOCPI.Analytics.sitesTree.getCheckedNodes();
		} else {
			if (window.sessionStorage.getItem(SPOCPI.Analytics.selectedSiteIdsLocalStorageKey) != null) {
				checkedNodeIds = window.sessionStorage.getItem(SPOCPI.Analytics.selectedSiteIdsLocalStorageKey).split(",").map(i => Number(i));
			}
		}

		return checkedNodeIds;
	},
	getAnalyticsResponseValue: function (obj, propertyName) {
		if (obj != undefined) {
			return obj["document"][propertyName];
		} else {
			return null;
		}
	}
};

SPOCPI.Analytics.init();