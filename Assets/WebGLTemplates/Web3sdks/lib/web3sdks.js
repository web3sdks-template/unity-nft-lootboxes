/// --- Web3sdks Brige ---
import { ethers } from "./ethers.js";
import { Web3sdksSDK } from "https://esm.sh/@web3sdks/sdk?bundle";

const separator = "/";
const subSeparator = "#";

// big number transform
const bigNumberReplacer = (key, value) => {
  // if we find a BigNumber then make it into a string (since that is safe)
  if (
    ethers.BigNumber.isBigNumber(value) ||
    (typeof value === "object" &&
      value !== null &&
      value.type === "BigNumber" &&
      "hex" in value)
  ) {
    return ethers.BigNumber.from(value).toString();
  }
  return value;
};

const w = window;
w.bridge = {};
w.bridge.initialize = (chain, options) => {
  console.debug("web3sdksSDK initialization:", chain, options);
  const sdk = new Web3sdksSDK(chain, JSON.parse(options));
  w.web3sdks = sdk;
};

const updateSDKSigner = () => {
  if (w.web3sdks) {
    const provider = new ethers.providers.Web3Provider(w.ethereum);
    w.web3sdks.updateSignerOrProvider(provider.getSigner());
  }
};

w.bridge.connect = async () => {
  if (w.ethereum) {
    await w.ethereum.enable;
    const provider = new ethers.providers.Web3Provider(w.ethereum);
    await provider.send("eth_requestAccounts", []);
    if (w.web3sdks) {
      updateSDKSigner();
      w.ethereum.on("accountsChanged", async (accounts) => {
        updateSDKSigner();
      });
      w.ethereum.on("chainChanged", async (chain) => {
        updateSDKSigner();
      });
      return await w.web3sdks.wallet.getAddress();
    } else {
      throw "window.web3sdks is not defined";
    }
  } else {
    throw "Please install a wallet browser extension";
  }
};

w.bridge.switchNetwork = async (chainId) => {
  if (chainId) {
    await window.ethereum.request({
      method: "wallet_switchEthereumChain",
      params: [{ chainId: "0x" + chainId.toString(16) }],
    });
    updateSDKSigner();
  } else {
    throw "Error Switching Network";
  }
};

w.bridge.invoke = async (route, payload) => {
  const routeArgs = route.split(separator);
  const firstArg = routeArgs[0].split(subSeparator);
  const addrOrSDK = firstArg[0];

  const fnArgs = JSON.parse(payload).arguments;
  const parsedArgs = fnArgs.map((arg) => {
    try {
      return typeof arg === "string" &&
        (arg.startsWith("{") || arg.startsWith("["))
        ? JSON.parse(arg)
        : arg;
    } catch (e) {
      return arg;
    }
  });
  console.debug("web3sdksSDK call:", route, parsedArgs);

  // wallet call
  if (addrOrSDK.startsWith("sdk")) {
    let prop = undefined;
    if (firstArg.length > 1) {
      prop = firstArg[1];
    }
    if (prop && routeArgs.length === 2) {
      const result = await w.web3sdks[prop][routeArgs[1]](...parsedArgs);
      return JSON.stringify({ result: result }, bigNumberReplacer);
    } else if (routeArgs.length === 2) {
      const result = await w.web3sdks[routeArgs[1]](...parsedArgs);
      return JSON.stringify({ result: result }, bigNumberReplacer);
    } else {
      throw "Invalid Route";
    }
  }

  // contract call
  if (addrOrSDK.startsWith("0x")) {
    let typeOrAbi = undefined;
    if (firstArg.length > 1) {
      try {
        typeOrAbi = JSON.parse(firstArg[1]); // try to parse ABI
      } catch (e) {
        typeOrAbi = firstArg[1];
      }
    }
    const contract = await w.web3sdks.getContract(addrOrSDK, typeOrAbi);
    if (routeArgs.length === 2) {
      const result = await contract[routeArgs[1]](...parsedArgs);
      return JSON.stringify({ result: result }, bigNumberReplacer);
    } else if (routeArgs.length === 3) {
      const result = await contract[routeArgs[1]][routeArgs[2]](...parsedArgs);
      return JSON.stringify({ result: result }, bigNumberReplacer);
    } else if (routeArgs.length === 4) {
      const result = await contract[routeArgs[1]][routeArgs[2]][routeArgs[3]](
        ...parsedArgs
      );
      return JSON.stringify({ result: result }, bigNumberReplacer);
    } else {
      throw "Invalid Route";
    }
  }
};
/// --- End Web3sdks Brige ---
