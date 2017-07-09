'use strict';

angular.module('dataTableModule', [])
    .directive('sbDataTable', function ($sce, $compile, $rootScope, $http, $timeout, $filter) {
        return {
            restrict: 'E',
            scope: {
                tableData: '=',
                widths: '=',
                cellTemplates: '=',
                injection: '=',
                selected: '=',
                hideTopPagination: '=',
                identifier: '@',
                subRowTemplate: '@',
                showSubRowTemplate: '=',
                validateSearch: '&',
                onDataFetched: '&',
                onRowExpand: '&'
            },
            controller: function ($scope, $element) {
                $scope.selected = [];
                $scope.showAdditionalFilters = false;

                var baseUrl = '/DynamicDataTable';

                $scope.headerPadding = 30;

                $scope.selectWidth = $scope.subRowTemplate != null && $scope.showSubRowTemplate ? 50 : 20;

                if ($scope.widths != null) {
                    $scope.tableWidth = $scope.widths.reduce(function (a, b) { return parseInt(a) + parseInt(b) + $scope.headerPadding; }) + $scope.selectWidth;
                }

                $scope.pageSize = 10;
                $scope.pageNumber = 1;
                $scope.request = {
                    SortColumn: "",
                    Identifier: $scope.identifier,
                    PageSize: $scope.pageSize,
                    PageNumber: $scope.pageNumber,
                    Asc: false,
                    SelectedFilters: []
                };

                $scope.getTableDefinition = function () {
                    $http({
                        url: baseUrl + '/GetTableDefinition',
                        params: { identifier: $scope.identifier },
                        method: 'GET'
                    }).then(function (response) {
                        $scope.tableDefinition = response.data;
                        $scope.request.PageSize = $scope.tableDefinition.PageSizeOptions[0];
                        $scope.request.SortColumn = $scope.tableDefinition.ColumnDefinitions[0].Identifier;
                        $scope.primary = $filter('filter')($scope.tableDefinition.ColumnDefinitions, { PrimaryKey: true })[0];

                        //$scope.showAdditionalFilters = $filter('filter')($scope.tableDefinition.FilterDefinitions, { Visible: true }).length > 0;

                        console.log(response);

                        $timeout(function () {
                            $(".date-picker").datepicker({
                                autoclose: true
                            });
                        });

                        $scope.getData();

                    }, function (error) {

                    });
                };

                $rootScope.$on('update-table-' + $scope.identifier, function (event, args) {
                    $scope.getData();
                });
                $rootScope.$on('reset-table-' + $scope.identifier, function (event, args) {
                    $scope.reset();
                });

                $scope.getData = function () {

                    if ($scope.injection != null) {
                        var injs = $scope.injection;

                        var cur = $filter('filter')($scope.request.SelectedFilters, { Injected: true });
                        for (var j = 0; j < cur.length; j++) {
                            $scope.request.SelectedFilters.splice($scope.request.SelectedFilters.indexOf(cur[j]), 1);
                        }

                        for (var i = 0; i < injs.length; i++) {
                            var inj = injs[i];
                            if (inj.FirstSelectedValue != null)
                                $scope.request.SelectedFilters.push({
                                    Identifier: inj.Identifier,
                                    SelectedOperator: inj.SelectedOperator,
                                    FirstSelectedValue: inj.FirstSelectedValue,
                                    SecondSelectedValue: inj.SecondSelectedValue,
                                    ColumnFilter: false,
                                    Injected: true
                                });
                        }
                    }

                    $http.post(baseUrl + '/GetTableData', $scope.request).then(function (response) {

                        if (response.data.Success) {
                            if ($scope.onDataFetched() != null) {
                                $timeout(function () {
                                    $scope.onDataFetched()();
                                }, 200);
                            }
                            console.log(response.data.PayLoad);

                            $scope.tableData = response.data.PayLoad.Data;
                            $scope.recordCount = response.data.PayLoad.TotalCount;
                            $scope.pageCount = Math.ceil($scope.recordCount / $scope.pageSize);

                            if ($scope.pageNumber > $scope.pageCount && $scope.pageCount != 0) {
                                $scope.pageNumber = $scope.pageCount == 0 ? 1 : $scope.pageCount;
                                $scope.request.PageNumber = $scope.pageNumber;
                                $scope.getData();
                            }
                        }
                        else {
                            toastr['error'](response.data.Error == null ? 'Error occured!' : response.data.Error);
                        }

                    }, function (error) {

                    });

                };
                $scope.select = function (row) {
                    if ($scope.selected.indexOf(row[$scope.primary.Identifier]) === -1) {
                        $scope.selected.push(row[$scope.primary.Identifier]);
                    }
                    else {
                        $scope.selected.splice($scope.selected.indexOf(row[$scope.primary.Identifier]), 1);
                    }
                };
                $scope.sortBy = function (column) {
                    if (column.Sortable == false) {
                        return;
                    }

                    $scope.request.Asc = $scope.request.SortColumn == column.Identifier ? !$scope.request.Asc : false;

                    $scope.request.SortColumn = column.Identifier;

                    $scope.getData();
                };
                $scope.updatePageSize = function () {
                    $scope.pageCount = Math.ceil($scope.recordCount / $scope.pageSize);
                    $scope.request.PageSize = $scope.pageSize;
                    $scope.getData();
                };
                $scope.updatePageNumber = function () {
                    $scope.request.PageNumber = $scope.pageNumber;
                    $scope.getData();
                };
                $scope.prevPage = function () {
                    $scope.pageNumber--;
                    $scope.updatePageNumber();
                };
                $scope.nextPage = function () {
                    $scope.pageNumber++;
                    $scope.updatePageNumber();
                };
                $scope.isSelected = function (row) {
                    if ($scope.selected.indexOf(row[$scope.primary.Identifier]) === -1) {
                        return false;
                    }
                    return true;
                };
                $scope.search = function () {

                    if ($scope.validateSearch() != null && $scope.validateSearch()($scope.tableDefinition) == false)
                        return;

                    $scope.request.SelectedFilters = [];

                    var colDefs = $scope.tableDefinition.ColumnDefinitions;
                    for (var i = 0; i < colDefs.length; i++) {
                        var col = colDefs[i];

                        if (col.FirstSelectedValue != null && col.FirstSelectedValue != '' || col.SecondSelectedValue != null && col.SecondSelectedValue != '') {

                            var firstValue = col.Type == 'Enum' ? col.FirstSelectedValue.Key : (col.Type == 'DateTime' && col.FirstSelectedValue != null && col.FirstSelectedValue != '' ? new Date(new Date(col.FirstSelectedValue).toUTCString()).toGMTString() : col.FirstSelectedValue);
                            var secondValue = (col.Type == "DateTime" && col.SecondSelectedValue != null && col.SecondSelectedValue != '') ? new Date(new Date(col.SecondSelectedValue).toUTCString()).toGMTString() : col.SecondSelectedValue;

                            $scope.request.SelectedFilters.push({
                                Identifier: col.Identifier,
                                SelectedOperator: col.Operator,
                                FirstSelectedValue: firstValue,
                                SecondSelectedValue: secondValue,
                                ColumnFilter: true
                            });
                        }
                    }


                    var colDefs = $scope.tableDefinition.FilterDefinitions;
                    for (var i = 0; i < colDefs.length; i++) {
                        var col = colDefs[i];

                        if (col.FirstSelectedValue != null && col.FirstSelectedValue != '' || col.SecondSelectedValue != null && col.SecondSelectedValue != '') {

                            var firstValue = col.Type == 'Enum' ? col.FirstSelectedValue : (col.Type == 'DateTime' && col.FirstSelectedValue != null && col.FirstSelectedValue != '' ? new Date(new Date(col.FirstSelectedValue).toUTCString()).toGMTString() : col.FirstSelectedValue);
                            var secondValue = (col.Type == "DateTime" && col.SecondSelectedValue != null && col.SecondSelectedValue != '') ? new Date(new Date(col.SecondSelectedValue).toUTCString()).toGMTString() : col.SecondSelectedValue;

                            $scope.request.SelectedFilters.push({
                                Identifier: col.Identifier,
                                SelectedOperator: col.Operator,
                                FirstSelectedValue: firstValue,
                                SecondSelectedValue: secondValue,
                                ColumnFilter: false
                            });
                        }
                    };



                    $scope.getData();

                };
                $scope.reset = function () {

                    var colDefs = $scope.tableDefinition.ColumnDefinitions;
                    for (var i = 0; i < colDefs.length; i++) {
                        var col = colDefs[i];
                        col.FirstSelectedValue = null;
                        col.SecondSelectedValue = null;
                    }

                    var colDefs = $scope.tableDefinition.FilterDefinitions;
                    for (var i = 0; i < colDefs.length; i++) {
                        var col = colDefs[i];
                        col.FirstSelectedValue = null;
                        col.SecondSelectedValue = null;
                    }
                    $scope.search();

                };
                $scope.selectAll = function () {

                    if ($scope.allSelected) {

                        $http.post(baseUrl + '/GetAllSelect', $scope.request).then(function (response) {

                            if (response.data.Success) {
                                $scope.selected = response.data.PayLoad;
                            }
                            else {
                                toastr['error'](response.data.Error == null ? 'Error occured!' : response.data.Error);
                            }
                        }, function (error) {
                            toastr['error']('Error occured!');
                        });

                    }
                    else {
                        $scope.selected = [];
                    }
                };
                $scope.expand = function (row) {

                    row.expanded = !row.expanded;
                    if ($scope.onRowExpand() != null) {
                        $scope.onRowExpand()(row);
                    }
                };


                $scope.getTableDefinition();
            },
            templateUrl: "/App/Utilities/DataTable/data-table.html"
        }
    }).filter("toDate", function () {
        var re = /\/Date\(([0-9]*)\)\//;
        return function (x) {
            if (x == null) {
                return null;
            }
            var m = x.match(re);
            if (m) return new Date(parseInt(m[1]));
            else return null;
        };
    }).filter("toLocal", function () {
        return function (x) {
            if (x == null || x == '') {
                return '';
            }
            var offset = new Date().getTimezoneOffset();
            return x.setMinutes(x.getMinutes() - offset);
        };
    }).directive('onEnter', function () {
        return function (scope, element, attrs) {
            element.bind("keydown keypress", function (event) {
                if (event.which === 13) {
                    scope.$apply(function () {
                        scope.$eval(attrs.onEnter);
                    });
                    event.preventDefault();
                }
            });
        };
    });
