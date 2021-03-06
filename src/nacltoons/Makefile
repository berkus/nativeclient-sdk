# Copyright (c) 2012 The Chromium Authors. All rights reserved.
# Use of this source code is governed by a BSD-style license that can be
# found in the LICENSE file.

ifndef BUILDBOT_BUILDERNAME
# By default, unless we are building on the buildbot set NACL_ARCH to so that
# we only build for a single architecture.
NACL_ARCH ?= x86_64
endif

DEBUG ?= 1
ifeq ($(DEBUG),1)
  CONFIG := Debug
else
  CONFIG := Release
endif

OUT_DIR := out
PUBLISH_DIR := $(OUT_DIR)/publish
TOOLCHAIN ?= newlib

all: cocos2dx lua-yaml
	@echo '@@@BUILD_STEP build game@@@'
	TOOLCHAIN=$(TOOLCHAIN) NACL_ARCH=$(NACL_ARCH) CONFIG=$(CONFIG) $(MAKE) -j10 -C proj.nacl

cocos2dx: build/build-cocos2dx.sh
	USE_BOX2D=1 NACL_ARCH=$(NACL_ARCH) build/build-cocos2dx.sh

lua-yaml:
	@echo '@@@BUILD_STEP lua-yaml@@@'
	TOOLCHAIN=$(TOOLCHAIN) CONFIG=$(CONFIG) NACL_ARCH=$(NACL_ARCH) $(MAKE) -f build/lua-yaml.mk

really-clean: clean
	$(RM) -r $(OUT_DIR)

clean:
	$(MAKE) -C proj.nacl clean

publish: all
	rm -fr $(PUBLISH_DIR)
	mkdir -p $(PUBLISH_DIR)
	cp $(OUT_DIR)/$(TOOLCHAIN)/$(CONFIG)/*.nexe $(PUBLISH_DIR)
	cp $(OUT_DIR)/$(TOOLCHAIN)/$(CONFIG)/*.nmf $(PUBLISH_DIR)
	if [ "$(TOOLCHAIN)" = "glibc" ]; then cp -r $(OUT_DIR)/$(TOOLCHAIN)/$(CONFIG)/lib* $(PUBLISH_DIR); fi
	ln -s $(PWD)/data/background.js $(PUBLISH_DIR)/background.js
	ln -s $(PWD)/data/common.js $(PUBLISH_DIR)/common.js
	ln -s $(PWD)/data/edit.js $(PUBLISH_DIR)/edit.js
	ln -s $(PWD)/data/index.html $(PUBLISH_DIR)/index.html
	ln -s $(PWD)/data/manifest.json $(PUBLISH_DIR)/manifest.json
	ln -s $(PWD)/data/res $(PUBLISH_DIR)/Resources

CHROME_ARGS += --no-first-run --user-data-dir=$(OUT_DIR)/user-data-dir

run: publish
	NACL_ARCH=$(NACL_ARCH) CHROME_ARGS="$(CHROME_ARGS)" $(MAKE) -C proj.nacl RUN

run-app: publish
	$(CHROME_PATH) $(CHROME_ARGS) --load-extension=$(PUBLISH_DIR) chrome://newtab

third_party/lua-yaml/yaml.so:
	make -C third_party/lua-yaml INC="-I/usr/include/lua5.1 -I."

test: third_party/lua-yaml/yaml.so validate
	third_party/lunit/lunit -i ./lua.sh tests/*_test.lua

validate: third_party/lua-yaml/yaml.so
	./lua.sh data/res/validate.lua data/res/sample_game/game.def

.PHONY: all lua-yaml cocos2dx clean publish run run-app really-clean test validate
