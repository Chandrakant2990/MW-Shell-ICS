'use strict';

var SPOCPI = SPOCPI || {};

SPOCPI.Subscription = {
	load: function () {
		var queryText = '';
		$(document).ready(function () {
			var table = $('#subscriptionsTable').DataTable({
				dom: 'Bfrtip',
				orderMulti: false,
				serverSide: true,
				processing: true,
				searching: true,
				initComplete: function () { SPOCPI.Subscription.onint(); },
				ajax: {
					url: '/api/subscriptions/search',
					method: 'GET',
					data: function (data) {
						data.orderBy = data.columns[data.order[0].column].data;
						data.orderDir = data.order[0].dir;
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
				lengthMenu: [
					[10, 25, 50, 100],
					['10 rows', '25 rows', '50 rows', '100 rows']
				],
				pageLength: 25,
				columnDefs: [{
					orderable: false,
					className: 'select-checkbox',
					targets: 0
				}],
				columns: [
					{
						"orderable": false,
						"data": null,
						"defaultContent": ''
					},
					{
						className: 'details-control',
						orderable: false,
						data: null,
						defaultContent: ''
					},
					{ data: 'document.rowKey' },
					{
						className: 'url-cell',
						data: 'document.libraryUrl',
						render: function (data, type, row, meta) {
							if (type === 'display') {
								data = '<a target="_blank" href="' + data + '">' + data + '</a>';
							}
							return data;
						}
					},
					{ data: 'document.description' },
					{
						data: 'document.creationDateTime'
					},
					{
						'orderable': false,
						'render': function (data, type, row, meta) {
							var colContent = "";
							if (row.document.isActive) {
								if (row.document.status === 'Disabled') {
									colContent += '<i title="Disabled Subscription" style="font-size: 20px; color:darkgrey; margin-right: 5px;" class="fa fa-check-circle"></i>';
								}
								else {
									colContent += '<i title="Active Subscription" style="font-size: 20px; color:#00a65a; margin-right: 5px;" class="fa fa-check-circle"></i>';
								}
							}
							if (row.document.status === 'Subscribed') {
								colContent += '<i title="Subscribed" style="font-size: 20px; color:#00a65a; margin-right: 5px;" class="fa fa-bolt"></i>';
							}
							return colContent;
						}
					},
					{
						'orderable': false,
						'render': function (data, type, row, meta) {
							var colContent = '';

							if (row.document.status === 'Subscribed') {
								colContent += "<button title='View tracking' type='button' class='btn btn-icon' id='viewTracking'><i class='fa fa-external-link-square' onclick='SPOCPI.Subscription.onViewTracking(&quot;" + row.document.rowKey + "&quot;,&quot;" + row.document.partitionKey + "&quot;,&quot;" + row.document.driveId + "&quot;)'></i></button>";
							} else {
								colContent += "<button title='View tracking' type='button' class='btn btn-icon' id='viewTracking' disabled='true'><i class='fa fa-external-link-square'></i></button>";
							}

							return colContent;
						}
					},
					{
						data: 'document.partitionKey', visible: false
					},
					{
						data: 'document.driveId', visible: false
					},
					{
						data: 'document.siteUrl', visible: false
					},
					{
						data: 'document.status', visible: false
					},
					{
						data: 'document.outputQueue', visible: false
					},
					{
						data: 'document.isActive', visible: false
					},
					{
						data: 'document.autoIndex', visible: false
					},
					{
						data: 'document.includeFolderRelativePath', visible: false
					}
				],
				order: [[5, 'desc']],
				select: {
					style: 'multi',
					selector: 'td:first-child'
				},
				buttons: [
					{
						text: 'Re-process Subscription(s)',
						titleAttr: 'Re-process selected subscription(s)',
						action: function (e, dt, node, config) {
							var count = table.rows({ selected: true }).count();
							var data = table.rows({ selected: true }).data();
							if (count && count > 0) {
								SPOCPI.Subscription.indexSubscriptions(table, data, 0);
							} else {
								SPOCPI.Subscription.errorMessageForNotSelectingSubscription();
							}
						}
					},
					{
						text: 'Disable Subscription(s)',
						titleAttr: 'Disable selected subscription(s)',
						action: function (e, dt, node, config) {
							var count = table.rows({ selected: true }).count();
							var data = table.rows({ selected: true }).data();
							if (count && count > 0) {
								SPOCPI.Subscription.disableSubscriptions(table, data, 1);
							} else {
								SPOCPI.Subscription.errorMessageForNotSelectingSubscription();
							}
						}
					},
					{
						text: 'Delete Subscription(s)',
						titleAttr: 'Delete selected subscription(s)',
						action: function (e, dt, node, config) {
							var count = table.rows({ selected: true }).count();
							var data = table.rows({ selected: true }).data();
							if (count && count > 0) {
								SPOCPI.Subscription.deleteSubscriptions(table, data, 2);
							} else {
								SPOCPI.Subscription.errorMessageForNotSelectingSubscription();
							}
						}
					},
					{
						extend: 'pageLength',
						titleAttr: 'Select number of items to display per page'
					},
					{
						text: 'Refresh',
						titleAttr: 'Refresh the Subscriptions table',
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

			$('#subscriptionsTable tbody').on('click', 'td.details-control', function () {
				var tr = $(this).closest('tr');
				var row = table.row(tr);

				if (row.child.isShown()) {
					// This row is already open - close it
					row.child.hide();
					tr.removeClass('shown');
				}
				else {
					// Open this row
					row.child(SPOCPI.Subscription.expandCollapseDetail(row.data())).show();
					tr.addClass('shown');
				}
			});

			if (!SPOCPI.Subscription.loggedInUserIsAdmin()) {
				SPOCPI.Subscription.enableDisableButton(table, false, [0, 1, 2]);
			}
		});
	},
	onint: function () {
		// take off all events from the searchfield
		$("#subscriptionsTable_wrapper input[type='search']").off();
		// Use return key to trigger search
		$("#subscriptionsTable_wrapper input[type='search']").on("keydown", function (evt) {
			if (evt.keyCode === 13) {
				$("#subscriptionsTable").DataTable().search($("input[type='search']").val()).draw();
			}
		});
		$("#search-btn").button().on("click", function () {
			$("#subscriptionsTable").DataTable().search($("input[type='search']").val()).draw();
		});
	},
	loggedInUserIsAdmin: function () {
		var userIsAdmin = $('#isAdmin').val();
		if (userIsAdmin) {
			try {
				userIsAdmin = JSON.parse(userIsAdmin);
			}
			catch (e) {
				userIsAdmin = true;
			}
		} else {
			userIsAdmin = false;
		}

		return userIsAdmin;
	},
	expandCollapseDetail: function format(d) {
		var isDisabled = d.document.status === "SubscriptionDisabled";
		var isEnabled = d.document.status === "Subscribed";
		var buttonsHtml = "";
		var userIsAdmin = SPOCPI.Subscription.loggedInUserIsAdmin();
		if (userIsAdmin) {
			if (!d.document.isActive) {
				buttonsHtml += "<button type=\"button\" class=\"btn btn-success btn-margin btn-enable-disable\" title=\"Activate the subscription\" id=\"btnActivateSubscription\" onclick=\"SPOCPI.Subscription.onActivate('" + d.document.rowKey + "','" + d.document.partitionKey + "','" + d.document.isActive + "')\">Activate</button>";
			} else {
				if (isDisabled || d.document.isActive === false)
					buttonsHtml += "<button type=\"button\" class=\"btn btn-success btn-margin btn-enable-disable\" title=\"Enable the subscription\" id=\"enableSub\" onclick=\"SPOCPI.Subscription.onDisable('" + d.document.rowKey + "','" + d.document.partitionKey + "','" + d.document.status + "')\">Enable</button>";
				if (isEnabled) {
					buttonsHtml += "<button type=\"button\" class=\"btn btn-primary btn-margin\" id=\"editSub\" onclick=\"SPOCPI.Subscription.onEdit('" + d.document.partitionKey + "', '" + d.document.rowKey + "','" + d.document.outputQueue + "','" + d.document.description + "', '" + encodeURI(d.document.parameters) + "')\">Edit</button>";
					buttonsHtml += "<button type=\"button\" class=\"btn btn-warning btn-margin btn-enable-disable\" title=\"Disable the subscription\" id=\"disableSub\" onclick=\"SPOCPI.Subscription.onDisable('" + d.document.rowKey + "','" + d.document.partitionKey + "','" + d.document.status + "')\">Disable</button>";
					buttonsHtml += "<button type=\"button\" class=\"btn btn-info btn-margin btn-enable-disable\" id=\"indexSub\" title=\"Re-index the library\" onclick=\"SPOCPI.Subscription.onIndex('" + d.document.partitionKey + "', '" + d.document.rowKey + "','" + d.document.spoSubscriptionId + "','" + d.document.driveId + "', '" + d.document.isActive + "', '" + d.document.autoIndex + "')\">Index</button>";
				}
				if (d.document.status !== 'Deleted') {
					buttonsHtml += "<button type=\"button\" class=\"btn btn-danger btn-enable-disable\" title=\"Delete the subscription\" id=\"deleteSub\" onclick=\"SPOCPI.Subscription.onDelete('" + d.document.rowKey + "','" + d.document.partitionKey + "','" + d.document.status + "','" + d.document.driveId + "')\">Delete</button>";
				}
			}
		}

		var siteUrl = '<a target="_blank" href="' + SPOCPI.Common.formatValue(d.document.siteUrl) + '">' + SPOCPI.Common.formatValue(d.document.siteUrl) + '</a>';
		var libraryUrl = '<a target="_blank" href="' + SPOCPI.Common.formatValue(d.document.libraryUrl) + '">' + SPOCPI.Common.formatValue(d.document.libraryUrl) + '</a>';

		var output = '<div class="box box-solid">';
		output += '<div class="box-body clearfix">';
		output += '<dl class="dl-horizontal">';
		output += '<dt title="Partition Key">Partition Key</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.partitionKey) + '</dd>';
		output += '<dt title="Drive Id">Drive Id</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.driveId) + '</dd>';
		output += '<dt title="SPO Subscription Id">SPO Subscription Id</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.spoSubscriptionId) + '</dd>';
		output += '<dt title="SPO Subscription Id">Site Url</dt>';
		output += '<dd>' + siteUrl + '</dd>';
		output += '<dt title="SPO Subscription Id">Library Url</dt>';
		output += '<dd>' + libraryUrl + '</dd>';
		output += '<dt title="Subscription Id">Subscription Status</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.status) + '</dd>';
		output += '<dt title="Activity Type">Activity Type</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.outputQueue) + '</dd>';
		output += '<dt title="Parameters">Parameters</dt>';
		output += '<dd>' + SPOCPI.Common.formatValue(d.document.parameters) + '</dd>';
		output += '<dt title="Is Active">Is Active</dt>';
		output += '<dd>' + d.document.isActive + '</dd>';
		output += '<dt title="Auto Index">Auto Index</dt>';
		output += '<dd>' + d.document.autoIndex + '</dd>';
		output += '<dt title="Folder Relative Path">Folder Relative Path</dt>';
		if (d.document.includeFolderRelativePath) {
			output += '<dd>' + d.document.includeFolderRelativePath + '</dd>';
		}
		else {
			output += '<dd>' + " " + '</dd>';
		}
		if (userIsAdmin && buttonsHtml) {
			output += '<dt title="Actions">Actions</dt>';
			output += '<dd>' + buttonsHtml + '</dd>';
		}
		output += '</dl>';
		output += '</div>';
		output += '</div>';

		return output;
	},
	onEdit: function (partitionKey, rowKey, outputQueue, description, parameters) {
		jQuery("#queueNamesAutoComplete").val(outputQueue);
		jQuery("#descriptionInput").val(description);
		jQuery("#parametersInput").val(decodeURI(parameters));
		$('#editSubscriptionModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				var subscription = { PartitionKey: partitionKey, RowKey: rowKey, OutputQueue: jQuery("#queueNamesAutoComplete").val(), Description: jQuery("#descriptionInput").val(), Parameters: jQuery("#parametersInput").val() };
				$.ajax({
					type: "POST",
					url: "/api/subscriptions/update",
					data: JSON.stringify(subscription),
					headers: {
						"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
					},
					dataType: "json",
					contentType: 'application/json; charset=utf-8',
					success: function (data) {
						$('#successModal').modal({
							keyboard: false
						})
							.on('click', '#close', function (e) {
								SPOCPI.Subscription.reloadTable();
							});
						$('.modal-body').html("Updated the details for the selected subscription.");
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
				});
			});
	},
	onDelete: function (rowKey, partitionKey, status, driveId) {
		$('#warningModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				var subscription = { RowKey: rowKey.trim(), PartitionKey: partitionKey.trim(), Status: status.trim(), DriveId: driveId.trim() };
				SPOCPI.Subscription.showLoader(2);
				SPOCPI.Subscription.enableDisableButtons(false);
				$.ajax({
					type: "DELETE",
					url: "/api/subscriptions",
					data: JSON.stringify(subscription),
					dataType: "json",
					contentType: 'application/json; charset=utf-8',
					success: function (data) {
						SPOCPI.Subscription.hideLoader(2);
						SPOCPI.Subscription.enableDisableButtons(true);
						$('#successModal').modal({
							keyboard: false
						})
							.on('click', '#close', function (e) {
								SPOCPI.Subscription.reloadTable();
							});
						$('.modal-body').html("The selected subscription is marked for deletion.");
					},
					error: function (xhr, textStatus, errorThrown) {
						try {
							SPOCPI.Subscription.hideLoader(2);
							SPOCPI.Subscription.enableDisableButtons(true);
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
				});
			});
		$('.modal-body').html("Are you sure you want to delete?");
	},
	onDisable: function (rowKey, partitionKey, status) {
		var subscription = { RowKey: rowKey.trim(), PartitionKey: partitionKey.trim(), Status: status.trim() };
		subscription = SPOCPI.Subscription.toggleStatus(subscription);
		$('#warningModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				SPOCPI.Subscription.showLoader(1);
				SPOCPI.Subscription.enableDisableButtons(false);
				$.ajax({
					type: "PATCH",
					url: "/api/subscriptions",
					data: JSON.stringify(subscription),
					dataType: "json",
					contentType: 'application/json; charset=utf-8',
					success: function (data) {
						SPOCPI.Subscription.hideLoader(1);
						SPOCPI.Subscription.enableDisableButtons(true);
						$('#successModal').modal({
							keyboard: false
						})
							.on('click', '#close', function (e) {
								SPOCPI.Subscription.reloadTable();
							});
						if (subscription.Status === 'Not Subscribed') {
							$('.modal-body').html("The selected subscription is Enabled");
						} else {
							$('.modal-body').html("The selected subscription is marked " + subscription.Status + ".");
						}
					},
					error: function (xhr, textStatus, errorThrown) {
						try {
							SPOCPI.Subscription.hideLoader(1);
							SPOCPI.Subscription.enableDisableButtons(true);
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
				});
			});
		var subscriptionStatus = '';
		if (subscription.Status) {
			subscriptionStatus = subscription.Status;
			if (subscription.Status === 'Disabled') {
				subscriptionStatus = 'Disable';
			} else if (subscription.Status === 'Not Subscribed') {
				subscriptionStatus = 'Enable Subscription';
			}
		}
		$('.modal-body').html("Are you sure you want to " + subscriptionStatus + "?");
	},
	onIndex: function (partitionKey, rowKey, spoSubscriptionId, driveId, isActive, autoIndex) {
		$('#indexLibrary').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				SPOCPI.Subscription.showLoader(0);
				SPOCPI.Subscription.enableDisableButtons(false);
				var subscription = {
					PartitionKey: partitionKey.trim(),
					RowKey: rowKey.trim(),
					SubscriptionId: spoSubscriptionId.trim(),
					SPOSubscriptionId: spoSubscriptionId.trim(),
					DriveId: driveId.trim(),
					IsActive: (isActive === 'true'),
					AutoIndex: (autoIndex === 'true')
				};
				var bypassSpoNotification = jQuery("#bypassSpo").prop('checked');
				var queueNames = jQuery("#queueNames").val();
				$.ajax({
					type: "POST",
					url: "/api/subscriptions/crawl",
					headers: {
						"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
					},
					data: JSON.stringify({ "subscription": subscription, "bypassSpoNotification": bypassSpoNotification, "queueNames": queueNames }),
					dataType: "json",
					contentType: 'application/json; charset=utf-8',
					success: function (data) {
						SPOCPI.Subscription.hideLoader(0);
						SPOCPI.Subscription.enableDisableButtons(true);
						var message = '';
						if (data) {
							$('#successModal').modal({
								keyboard: false
							})
								.on('click', '#close', function (e) {
									SPOCPI.Subscription.reloadTable();
								});
							message = 'The selected subscription is marked for indexing.';
							$('.modal-body').html(message);
						}
					},
					error: function (xhr, textStatus, errorThrown) {
						try {
							SPOCPI.Subscription.hideLoader(0);
							SPOCPI.Subscription.enableDisableButtons(true);
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
				});
			});
	},
	checkForUpdates: function (rowKey, spoSubscriptionId, driveId) {
		$('#warningModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				var subscription = {
					RowKey: rowKey.trim(),
					SubscriptionId: spoSubscriptionId.trim(),
					SPOSubscriptionId: spoSubscriptionId.trim(),
					DriveId: driveId.trim()
				};
				subscription = SPOCPI.Subscription.toggleStatus(subscription);
				$.ajax({
					type: "POST",
					url: "/api/subscriptions/runLastCrawl",
					data: JSON.stringify(subscription),
					dataType: "json",
					headers: {
						"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
					},
					contentType: 'application/json; charset=utf-8',
					success: function (data) {
						var message = '';
						if (data) {
							$('#successModal').modal({
								keyboard: false
							})
								.on('click', '#close', function (e) {
									SPOCPI.Subscription.reloadTable();
								});
							message = 'The selected subscription is marked for updates.';
							$('.modal-body').html(message);
						}
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
				});
			});
		$('.modal-body').html("Are you sure you want to check for updates?");
	},
	onActivate: function (rowKey, partitionKey, isActive) {
		$('#warningModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				try {
					var subscription = {
						RowKey: rowKey.trim(),
						PartitionKey: partitionKey.trim(),
						IsActive: JSON.parse(isActive)
					};
					subscription = SPOCPI.Subscription.toggleStatus(subscription);
					$.ajax({
						type: "POST",
						url: "/api/subscriptions/ActivateSubscription",
						headers: {
							"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
						},
						data: JSON.stringify(subscription),
						dataType: "json",
						contentType: 'application/json; charset=utf-8',
						success: function (data) {
							var message = '';
							$('#successModal').modal({
								keyboard: false
							})
								.on('click', '#close', function (e) {
									SPOCPI.Subscription.reloadTable();
								});
							message = 'The selected subscription is activated';
							$('.modal-body').html(message);
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
					});
				} catch (e) {
					console.error(e);
				}
			});
		$('.modal-body').html("Are you sure you want to activate the subscription for future processing?");
	},
	onViewTracking: function (rowKey, partitionKey, driveId) {
		var subscription = { RowKey: rowKey.trim(), PartitionKey: partitionKey.trim(), DriveId: driveId.trim() };
		location.href = "/tracking/index?driveId=" + encodeURIComponent(driveId);
	},
	toggleStatus: function (subscription) {
		if (subscription.Status === "Subscribed")
			subscription.Status = "Disabled";
		else if (subscription.Status === "SubscriptionDisabled")
			subscription.Status = "Not Subscribed";

		return subscription;
	},
	indexSubscriptions: function (table, data, buttonIndex) {
		$('#warningModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				try {
					var subscriptionEntities = [];
					var callSuccess = true;
					if (data && data.length > 0) {
						for (var i = 0; i < data.length; i++) {
							if (data[i].document.rowKey && data[i].document.spoSubscriptionId && data[i].document.driveId) {
								subscriptionEntities.push({
									RowKey: data[i].document.rowKey.trim(),
									SubscriptionId: data[i].document.spoSubscriptionId.trim(),
									DriveId: data[i].document.driveId,
									SPOSubscriptionId: data[i].document.spoSubscriptionId.trim()
								});
							} else {
								callSuccess = false;
								var info = 'Information missing for the selected item number: ' + (i + 1);
								$('#dangerModal').modal({
									keyboard: false
								});
								$('.modal-body').html(info);
							}
						}
						SPOCPI.Subscription.unselectItems(table);
						if (callSuccess) {
							SPOCPI.Subscription.showLoaderProcessing(table, buttonIndex);
							SPOCPI.Subscription.enableDisableButton(table, false, [0, 1, 2]);
							$.ajax({
								type: 'POST',
								url: '/api/subscriptions/crawlSubscriptions',
								data: JSON.stringify(subscriptionEntities),
								headers: {
									"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
								},
								dataType: "json",
								contentType: 'application/json; charset=utf-8',
								success: function (data) {
									SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
									SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
									var success = false;
									if (data && data.length > 0) {
										for (var i = 0; i < data.length; i++) {
											if (data[i] && data[i].statusCode === 200) {
												success = true;
											} else {
												success = false;
											}
										}
										if (success) {
											$('#successModal').modal({
												keyboard: false
											})
												.on('click', '#close', function (e) {
													SPOCPI.Subscription.reloadTable();
												});
											$('.modal-body').html("The selected subscription(s) are marked for indexing.");
										}
									}
								},
								error: function (xhr, textStatus, errorThrown) {
									SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
									SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
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
							});
						}
					}
				} catch (e) {
					console.error(e);
					SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
					SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
				}
			})
			.on('click', '#close', function (e) {
				try {
					SPOCPI.Subscription.unselectItems(table);
				} catch (e) {
					console.error(e);
				}
			});
		$('.modal-body').html("Are you sure you want to index?");
	},
	deleteSubscriptions: function (table, data, buttonIndex) {
		$('#warningModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				try {
					var subscriptionEntities = [];
					var callSuccess = true;
					var missingInformationItems = [];
					if (data && data.length > 0) {
						for (var i = 0; i < data.length; i++) {
							if (data[i].document.rowKey && data[i].document.partitionKey && data[i].document.libraryUrl && data[i].document.status) {
								subscriptionEntities.push({
									RowKey: data[i].document.rowKey,
									PartitionKey: data[i].document.partitionKey,
									LibraryUrl: data[i].document.libraryUrl,
									Status: data[i].document.status,
									DriveId: data[i].document.driveId
								});
							} else {
								callSuccess = false;
								missingInformationItems.push((i + 1));
							}
						}
						if (callSuccess) {
							SPOCPI.Subscription.unselectItems(table);
							SPOCPI.Subscription.showLoaderProcessing(table, buttonIndex);
							SPOCPI.Subscription.enableDisableButton(table, false, [0, 1, 2]);
							$.ajax({
								type: 'POST',
								url: '/api/subscriptions/deleteSubscriptions',
								data: JSON.stringify(subscriptionEntities),
								headers: {
									"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
								},
								dataType: "json",
								contentType: 'application/json; charset=utf-8',
								success: function (data) {
									SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
									SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
									var success = false;
									if (data && data.length > 0) {
										for (var i = 0; i < data.length; i++) {
											if (data[i]) {
												success = true;
											} else {
												success = false;
											}
										}
										if (success) {
											$('#successModal').modal({
												keyboard: false
											})
												.on('click', '#close', function (e) {
													SPOCPI.Subscription.reloadTable();
												});
											$('.modal-body').html("The selected subscription(s) are marked for deletion.");
										}
									}
								},
								error: function (xhr, textStatus, errorThrown) {
									SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
									SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
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
							});
						} else {
							if (missingInformationItems && missingInformationItems.length > 0) {
								var info = '<h4>Information missing for the following selected item number(s): </h4>';
								info += '<ul class="list-group">';
								info += missingInformationItems.join(' , ');
								info += '</ul>';
								$('#dangerModal').modal({
									keyboard: false
								});
								$('.modal-body').html(info);
							}
						}
					}
				} catch (e) {
					console.error(e);
					SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
					SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
				}
			})
			.on('click', '#close', function (e) {
				try {
					SPOCPI.Subscription.unselectItems(table);
				} catch (e) {
					console.error(e);
				}
			});
		$('.modal-body').html("Are you sure you want to delete?");
	},
	disableSubscriptions: function (table, data, buttonIndex) {
		$('#warningModal').modal({
			keyboard: false
		})
			.on('click', '#yes', function (e) {
				try {
					var subscriptionEntities = [];
					var callSuccess = true;
					var subscription = {};
					if (data && data.length > 0) {
						for (var i = 0; i < data.length; i++) {
							if (data[i].document.rowKey && data[i].document.partitionKey && data[i].document.libraryUrl && data[i].document.status) {
								subscription = {
									RowKey: data[i].document.rowKey.trim(),
									PartitionKey: data[i].document.partitionKey.trim(),
									Status: data[i].document.status.trim()
								};
								subscription = SPOCPI.Subscription.toggleStatus(subscription);
								subscriptionEntities.push(subscription);
							} else {
								callSuccess = false;
								var info = 'Missing information for the selected item number: ' + (i + 1);
								$('#dangerModal').modal({
									keyboard: false
								});
								$('.modal-body').html(info);
							}
						}
						if (callSuccess) {
							SPOCPI.Subscription.unselectItems(table);
							SPOCPI.Subscription.showLoaderProcessing(table, buttonIndex);
							SPOCPI.Subscription.enableDisableButton(table, false, [0, 1, 2]);
							$.ajax({
								type: 'POST',
								url: '/api/subscriptions/disableSubscriptions',
								data: JSON.stringify(subscriptionEntities),
								headers: {
									"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
								},
								dataType: "json",
								contentType: 'application/json; charset=utf-8',
								success: function (data) {
									SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
									SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
									var success = false;
									if (data && data.length > 0) {
										for (var i = 0; i < data.length; i++) {
											if (data[i]) {
												success = true;
											} else {
												success = false;
											}
										}
										if (success) {
											$('#successModal').modal({
												keyboard: false
											})
												.on('click', '#close', function (e) {
													SPOCPI.Subscription.reloadTable();
												});
											$('.modal-body').html("The selected subscription(s) are marked disabled.");
										}
									}
								},
								error: function (xhr, textStatus, errorThrown) {
									SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
									SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
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
							});
						}
					}
				} catch (e) {
					console.error(e);
					SPOCPI.Subscription.hideLoaderProcessing(table, buttonIndex);
					SPOCPI.Subscription.enableDisableButton(table, true, [0, 1, 2]);
				}
			})
			.on('click', '#close', function (e) {
				try {
					SPOCPI.Subscription.unselectItems(table);
				} catch (e) {
					console.error(e);
				}
			});
		$('.modal-body').html("Are you sure you want to disable?");
	},
	unselectItems: function (table) {
		try {
			table.rows({ selected: true }).deselect();
		} catch (e) {
			console.error(e);
		}
	},
	enableDisableButton: function (table, enableDisable, buttonIds) {
		try {
			if (buttonIds && buttonIds.length > 0) {
				for (var i = 0; i < buttonIds.length; i++) {
					table.button(buttonIds[i]).enable(enableDisable);
				}
			}
		} catch (e) {
			console.error(e);
		}
	},
	enableDisableButtons: function (enableDisable) {
		try {
			var table = $('#subscriptionsTable').DataTable();
			table.button(0).enable(enableDisable);
			table.button(1).enable(enableDisable);
			table.button(2).enable(enableDisable);
			$("button.btn-enable-disable").attr("disabled", !enableDisable);
		} catch (e) {
			console.error(e);
		}
	},
	errorMessageForNotSelectingSubscription: function () {
		try {
			$('#dangerModal').modal({
				keyboard: false
			});
			$('.modal-body').html("Please select subscription(s) to proceed");
		} catch (e) {
			console.error(e);
		}
	},
	reloadTable: function () {
		try {
			setTimeout(function () {
				window.location.reload(true);
			}, 500);
		} catch (e) {
			console.error(e);
		}
	},
	hideLoaderProcessing: function (table, buttonIndex) {
		table.button(parseInt(buttonIndex)).processing(false);
	},
	showLoaderProcessing: function (table, buttonIndex) {
		table.button(parseInt(buttonIndex)).processing(true);
	},
	showLoader: function (buttonIndex) {
		var table = $('#subscriptionsTable').DataTable();
		table.button(parseInt(buttonIndex)).processing(true);
	},
	hideLoader: function (buttonIndex) {
		var table = $('#subscriptionsTable').DataTable();
		table.button(parseInt(buttonIndex)).processing(false);
	}
};

SPOCPI.Subscription.load();