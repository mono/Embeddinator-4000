#!/bin/bash -e

set -o pipefail

FAIL=
PROVISION_DOWNLOAD_DIR=/tmp/x-provisioning

# parse command-line arguments
while ! test -z $1; do
	case $1 in
		--provision)
			PROVISION_MONO=1
			shift
			;;
		--provision-mono)
			PROVISION_MONO=1
			shift
			;;
		--provision-all)
			PROVISION_MONO=1
			shift
			;;
		--ignore-mono)
			IGNORE_MONO=1
			shift
			;;
		*)
			echo "Unknown argument: $1"
			exit 1
			;;
	esac
done

# reporting functions
function fail () {
	tput setaf 1 2>/dev/null || true
	echo "    $1"
	tput sgr0 2>/dev/null || true
	FAIL=1
}

function warn () {
	tput setaf 3 2>/dev/null || true
	echo "    $1"
	tput sgr0 2>/dev/null || true
}

function ok () {
	echo "    $1"
}

function log () {
	echo "        $1"
}

# $1: the version to check
# $2: the minimum version to check against
function is_at_least_version () {
	ACT_V=$1
	MIN_V=$2

	if [[ "$ACT_V" == "$MIN_V" ]]; then
		return 0
	fi

	IFS=. read -a V_ACT <<< "$ACT_V"
	IFS=. read -a V_MIN <<< "$MIN_V"
	
	# get the minimum # of elements
	AC=${#V_ACT[@]}
	MC=${#V_MIN[@]}
	COUNT=$(($AC>$MC?$MC:$AC))

	C=0
	while (( $C < $COUNT )); do
		ACT=${V_ACT[$C]}
		MIN=${V_MIN[$C]}
		if (( $ACT > $MIN )); then
			return 0
		elif (( "$MIN" > "$ACT" )); then
			return 1
		fi
		let C++
	done

	if (( $AC == $MC )); then
		# identical?
		return 0
	fi

	if (( $AC > $MC )); then
		# more version fields in actual than min: OK
		return 0
	elif (( $AC == $MC )); then
		# entire strings aren't equal (first check in function), but each individual field is?
		return 0
	else
		# more version fields in min than actual (1.0 vs 1.0.1 for instance): not OK
		return 1
	fi
}

function install_mono () {
	local MONO_URL=`grep MIN_MONO_URL= Make.config | sed 's/.*=//'`
	local MIN_MONO_VERSION=`grep MIN_MONO_VERSION= Make.config | sed 's/.*=//'`

	if test -z $MONO_URL; then
		fail "No MIN_MONO_URL set in Make.config, cannot provision"
		return
	fi

	mkdir -p $PROVISION_DOWNLOAD_DIR
	log "Downloading Mono $MIN_MONO_VERSION from $MONO_URL to $PROVISION_DOWNLOAD_DIR..."
	local MONO_NAME=`basename $MONO_URL`
	local MONO_PKG=$PROVISION_DOWNLOAD_DIR/$MONO_NAME
	curl -L $MONO_URL > $MONO_PKG

	log "Installing Mono $MIN_MONO_VERSION from $MONO_URL..."
	sudo installer -pkg $MONO_PKG -target /

	rm -f $MONO_PKG
}

function check_mono () {
	if ! test -z $IGNORE_MONO; then return; fi

	PKG_CONFIG_PATH=/Library/Frameworks/Mono.framework/Versions/Current/bin/pkg-config
	if ! /Library/Frameworks/Mono.framework/Commands/mono --version 2>/dev/null >/dev/null; then
		if ! test -z $PROVISION_MONO; then
			install_mono
		else
			fail "You must install the Mono MDK (http://www.mono-project.com/download/)"
			return
		fi
	elif ! test -e $PKG_CONFIG_PATH; then
		if ! test -z $PROVISION_MONO; then
			install_mono
		else
			fail "Could not find pkg-config, you must install the Mono MDK (http://www.mono-project.com/download/)"
			return
		fi
	fi

	MIN_MONO_VERSION=`grep MIN_MONO_VERSION= Make.config | sed 's/.*=//'`
	MAX_MONO_VERSION=`grep MAX_MONO_VERSION= Make.config | sed 's/.*=//'`

	ACTUAL_MONO_VERSION=`$PKG_CONFIG_PATH --modversion mono`.`cat /Library/Frameworks/Mono.framework/Home/updateinfo | cut -d' ' -f2 | cut -c6- | awk '{print(int($0))}'`
	if ! is_at_least_version $ACTUAL_MONO_VERSION $MIN_MONO_VERSION; then
		if ! test -z $PROVISION_MONO; then
			install_mono
			ACTUAL_MONO_VERSION=`$PKG_CONFIG_PATH --modversion mono`
		else
			fail "You must have at least Mono $MIN_MONO_VERSION, found $ACTUAL_MONO_VERSION"
			return
		fi
	elif [[ "$ACTUAL_MONO_VERSION" == "$MAX_MONO_VERSION" ]]; then
		: # this is ok
	elif is_at_least_version $ACTUAL_MONO_VERSION $MAX_MONO_VERSION; then
		if ! test -z $PROVISION_MONO; then
			install_mono
			ACTUAL_MONO_VERSION=`$PKG_CONFIG_PATH --modversion mono`.`cat /Library/Frameworks/Mono.framework/Home/updateinfo | cut -d' ' -f2 | cut -c6- | awk '{print(int($0))}'`
		else
			fail "Your mono version is too new, max version is $MAX_MONO_VERSION, found $ACTUAL_MONO_VERSION."
			fail "You may edit Make.config and change MAX_MONO_VERSION to your actual version to continue the"
			fail "build (unless you're on a release branch). Once the build completes successfully, please"
			fail "commit the new MAX_MONO_VERSION value."
			return
		fi
	fi

	ok "Found Mono $ACTUAL_MONO_VERSION (at least $MIN_MONO_VERSION and not more than $MAX_MONO_VERSION is required)"
}

echo "Checking system..."

check_mono

if test -z $FAIL; then
	echo "System check succeeded"
else
	echo "System check failed"
	exit 1
fi
