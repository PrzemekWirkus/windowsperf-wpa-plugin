﻿// BSD 3-Clause License
//
// Copyright (c) 2022, Arm Limited
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
//
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using WPAPlugin.Constants;
using WPAPlugin.DataCookers;
using WPAPlugin.Events;

namespace WPAPlugin.Tables
{
    [Table]
    public static class WperfCountingTableFromDataCooker
    {
        public static TableDescriptor TableDescriptor =>
            new TableDescriptor(
                Guid.NewGuid(),
                "Counting",
                "Counting parsed from wperf JSON output",
                requiredDataCookers: new List<DataCookerPath>
                {
                    WperfPluginConstants.CountCookerPath
                },
                defaultLayout: TableLayoutStyle.Table
            );

        private static readonly ColumnConfiguration CoreColumn = new ColumnConfiguration(
            new ColumnMetadata(
                Guid.NewGuid(),
                "Core",
                "Core Number"
            )
        );
        private static readonly ColumnConfiguration ValueColumn = new ColumnConfiguration(
            new ColumnMetadata(
                Guid.NewGuid(),
                "Value",
                "Value Number"
            ),
            new UIHints { AggregationMode = AggregationMode.Sum }
        );

        private static readonly ColumnConfiguration EventNameColumn = new ColumnConfiguration(
            new ColumnMetadata(
                Guid.NewGuid(),
                "Name",
                "Event Name"
            )
        );
        private static readonly ColumnConfiguration EventIndexColumn = new ColumnConfiguration(
            new ColumnMetadata(
                Guid.NewGuid(),
                "Index",
                "Event Index"
            )
        );
        private static readonly ColumnConfiguration EventNoteColumn = new ColumnConfiguration(
            new ColumnMetadata(
                Guid.NewGuid(),
                "Note",
                "Event Note"
            )
        );

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            IReadOnlyList<WperfEvent> lineItems = tableData.QueryOutput<
                IReadOnlyList<WperfEvent>
            >(
                new DataOutputPath(
                    WperfPluginConstants.CountCookerPath,
                    nameof(WperfCountDataCooker.WperfEvents)
                )
            );

            IProjection<int, WperfEvent> baseProjection = Projection.Index(lineItems);
            IProjection<int, int> coreProjection = baseProjection.Compose(el => el.CoreNumber);
            IProjection<int, string> nameProjection = baseProjection.Compose(el => el.Name);
            IProjection<int, double> valueProjection = baseProjection.Compose(el => el.Value);
            IProjection<int, string> indexProjection = baseProjection.Compose(el => el.Index);
            IProjection<int, string> noteProjection = baseProjection.Compose(el => el.Note);

            TableConfiguration groupByCoreConfig = new TableConfiguration("Group by core")
            {
                Columns = new[]
                {
                    CoreColumn,
                    EventNameColumn,
                    TableConfiguration.PivotColumn,
                    EventNoteColumn,
                    ValueColumn,
                },
            };

            TableConfiguration groupByEventConfig = new TableConfiguration("Group by event")
            {
                Columns = new[]
                {
                    EventNameColumn,
                    CoreColumn,
                    TableConfiguration.PivotColumn,
                    EventNoteColumn,
                    ValueColumn,
                },
            };

            _ = tableBuilder
                .AddTableConfiguration(groupByCoreConfig)
                .AddTableConfiguration(groupByEventConfig)
                .SetDefaultTableConfiguration(groupByEventConfig)
                .SetRowCount(lineItems.Count)
                .AddColumn(CoreColumn, coreProjection)
                .AddColumn(EventNameColumn, nameProjection)
                .AddColumn(ValueColumn, valueProjection)
                .AddColumn(EventIndexColumn, indexProjection)
                .AddColumn(EventNoteColumn, noteProjection);
        }
    }
}
