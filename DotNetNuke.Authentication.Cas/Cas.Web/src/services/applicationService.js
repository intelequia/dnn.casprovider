import util from "../utils";
class ApplicationService {
    getServiceFramework(controller) {
        let sf = util.utilities.sf;
        sf.controller = controller;
        return sf;
    }

    getSettings(callback) {
        const sf = this.getServiceFramework("Cas");        
        sf.get("GetSettings", {}, callback);
    }

    updateSettings(payload, callback, failureCallback) {
        const sf = this.getServiceFramework("Cas");        
        sf.post("UpdateSettings", payload, failureCallback, callback);
    }    
}
const applicationService = new ApplicationService();
export default applicationService;