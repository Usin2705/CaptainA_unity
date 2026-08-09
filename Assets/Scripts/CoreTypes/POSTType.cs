public enum POSTType
{
    MDD_TASK,
    LLM_TASK,
    PuheNumero_TASK,
    ASA_CONSENT,
    ASA_TASK,
    USER_ASA_FEEDBACK,
    ASA_PROFILE,

    // POST /request/user with type=delete - the user asking for their data to be erased.
    DATA_DEL_REQUEST,
    OTHER,
}
