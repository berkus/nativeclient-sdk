# Copyright (c) 2011 The Chromium Authors. All rights reserved.
# Use of this source code is governed by a BSD-style license that can be
# found in the LICENSE file.

# Documentation on PRESUBMIT.py can be found at:
# http://www.chromium.org/developers/how-tos/depottools/presubmit-scripts

EXCLUDED_PATHS = (
    r"nacltoons/bindings/.*",
)

def CheckChangeOnUpload(input_api, output_api):
  report = []
  affected_files = input_api.AffectedFiles(include_deletes=False)
  report.extend(input_api.canned_checks.PanProjectChecks(
      input_api, output_api, excluded_paths=EXCLUDED_PATHS))
  return report


def CheckChangeOnCommit(input_api, output_api):
  report = []
  report.extend(CheckChangeOnUpload(input_api, output_api))

# Disable presubmit until we fix tree status
#  report.extend(input_api.canned_checks.CheckTreeIsOpen(
#      input_api, output_api,
#      json_url='http://naclsdk-status.appspot.com/current?format=json'))
  return report
